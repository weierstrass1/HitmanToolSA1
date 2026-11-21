"""Small native-mode 65816 contract runner for assembled Hitman routines.

Not a SNES emulator: no PPU, interrupts, DMA or cycle/timing claims. Unknown
opcodes fail immediately. Tests execute Asar's bytes, not Python collision code.
"""

class CPU:
    def __init__(self, rom, labels, sa1=False):
        self.rom, self.labels, self.sa1 = rom, labels, sa1
        self.mem = {}
        self.a = self.x = self.y = self.db = self.pb = self.pc = 0
        self.p = 0x30
        self.d = 0x3000 if sa1 else 0
        self.s = 0x37ff if sa1 else 0x1fff
        self.trace = []
        self.stubs = {}

    def key(self, address):
        address &= 0xffffff
        bank, off = address >> 16, address & 0xffff
        if bank & 0x7f < 0x40:
            if self.sa1:
                if off < 0x800 or 0x3000 <= off < 0x3800:
                    return 0x003000 + (off & 0x7ff)
                if 0x6000 <= off < 0x8000:
                    return 0x400000 + off - 0x6000
            elif off < 0x2000:
                return 0x7e0000 + off
        return address

    def read(self, address, width=1):
        def byte(address):
            key = self.key(address)
            if key in self.mem:
                return self.mem[key]
            bank, off = address >> 16, address & 0xffff
            if off >= 0x8000 and (bank & 0x7f) < 0x40:
                index = (bank & 0x7f) * 0x8000 + (off & 0x7fff)
                return self.rom[index] if index < len(self.rom) else 0
            if self.sa1 and bank >= 0xc0:
                index = (bank - 0xc0) * 0x10000 + off
                return self.rom[index] if index < len(self.rom) else 0
            return 0
        return sum(byte((address + i) & 0xffffff) << (i * 8) for i in range(width))

    def write(self, address, value, width=1):
        for i in range(width):
            self.mem[self.key(address + i)] = (value >> (8*i)) & 255

    def fetch(self, width=1):
        value = self.read(self.pb << 16 | self.pc, width)
        self.pc = (self.pc + width) & 65535
        return value

    def push(self, value, width=1):
        for i in reversed(range(width)):
            self.write(self.s, value >> (8*i))
            self.s = (self.s-1) & 65535

    def pop(self, width=1):
        result = 0
        for i in range(width):
            self.s = (self.s+1) & 65535
            result |= self.read(self.s) << (i*8)
        return result

    def flag(self, bit, value):
        self.p = self.p | bit if value else self.p & ~bit

    def nz(self, value, width):
        value &= (1 << (width*8))-1
        self.flag(2, value == 0)
        self.flag(128, value & (1 << (width*8-1)))
        return value

    def mode(self, value):
        self.p = value & 255
        if self.p & 16:
            self.x &= 255
            self.y &= 255

    def run(self, name, short=False, bank=None, limit=100000):
        start = self.labels[name] if isinstance(name, str) else name
        self.pb, self.pc = start >> 16, start & 65535
        initial_s = self.s
        # Stop on an artificial return site outside the assembled code.
        return_bank = bank if bank is not None else self.pb
        stop = return_bank << 16 | 0x7fff
        if not short:
            self.push(return_bank)
        self.push(0x7ffe, 2)
        for _ in range(limit):
            if self.pb << 16 | self.pc == stop:
                assert self.s == initial_s, (name, hex(self.s), hex(initial_s))
                return
            address = self.pb << 16 | self.pc
            self.trace.append(address)
            if address in self.stubs:
                stub = self.stubs[address]
                short_stub = isinstance(stub, tuple)
                (stub[0] if short_stub else stub)(self)
                self.pc = (self.pop(2)+1) & 65535
                if not short_stub:
                    self.pb = self.pop()
                continue
            self.step()
        raise AssertionError((name, 'instruction limit', [hex(a) for a in self.trace[-12:]]))

    def step(self):
        address = self.pb << 16 | self.pc
        op = self.fetch()
        aw, iw = (1 if self.p & 32 else 2), (1 if self.p & 16 else 2)
        mask = (1 << (aw*8))-1
        if op in (0xc2, 0xe2):
            bits = self.fetch()
            self.mode(self.p & ~bits if op == 0xc2 else self.p | bits)
            return
        if op in (0x18, 0x38): self.flag(1, op == 0x38); return
        if op == 0xea: return
        if op in (0x48,0xda,0x5a,0x8b,0x4b,0x08):
            val,w = {0x48:(self.a,aw),0xda:(self.x,iw),0x5a:(self.y,iw),0x8b:(self.db,1),0x4b:(self.pb,1),0x08:(self.p,1)}[op]
            self.push(val,w); return
        if op in (0x68,0xfa,0x7a,0xab,0x28):
            if op == 0x28: self.mode(self.pop()); return
            w = aw if op == 0x68 else (1 if op == 0xab else iw)
            v = self.nz(self.pop(w),w)
            if op == 0x68: self.a = (self.a & 0xff00 | v) if aw == 1 else v
            elif op == 0xfa: self.x = v
            elif op == 0x7a: self.y = v
            else: self.db = v
            return
        if op == 0xf4: self.push(self.fetch(2),2); return
        if op == 0x22:
            dest = self.fetch(3); self.push(self.pb); self.push((self.pc-1)&65535,2)
            self.pb,self.pc = dest>>16,dest&65535; return
        if op == 0x20:
            dest=self.fetch(2); self.push((self.pc-1)&65535,2); self.pc=dest; return
        if op in (0x60,0x6b):
            self.pc=(self.pop(2)+1)&65535
            if op==0x6b: self.pb=self.pop()
            return
        if op in (0x4c,0x5c,0x7c,0xdc):
            if op==0x4c: self.pc=self.fetch(2)
            elif op==0x5c:
                v=self.fetch(3); self.pb,self.pc=v>>16,v&65535
            elif op==0x7c: self.pc=self.read(self.pb<<16|((self.fetch(2)+self.x)&65535),2)
            else:
                v=self.read(self.fetch(2),3); self.pb,self.pc=v>>16,v&65535
            return
        if op in (0x80,0x90,0xb0,0xf0,0xd0,0x10,0x30,0x50,0x70,0x82):
            w=2 if op==0x82 else 1; offset=self.fetch(w); sign=1<<(8*w-1)
            offset=offset-(sign*2) if offset&sign else offset
            bit,isset={0x90:(1,False),0xb0:(1,True),0xf0:(2,True),0xd0:(2,False),0x10:(128,False),0x30:(128,True),0x50:(64,False),0x70:(64,True)}.get(op,(0,False))
            if not bit or bool(self.p&bit)==isset: self.pc=(self.pc+offset)&65535
            return
        if op in (0xaa,0xa8,0x8a,0x98,0x9b,0xbb,0xeb):
            if op==0xeb:
                self.a=(self.a>>8|self.a<<8)&65535;self.nz(self.a&255,1);return
            if op in (0xaa,0xa8): v=self.nz(self.a,iw); setattr(self,'x' if op==0xaa else 'y',v)
            elif op in (0x9b,0xbb): setattr(self,'y' if op==0x9b else 'x',self.nz(self.x if op==0x9b else self.y,iw))
            else:
                v=self.nz(self.x if op==0x8a else self.y,aw);self.a=(self.a&0xff00|v) if aw==1 else v
            return
        if op in (0xe8,0xca,0xc8,0x88):
            reg='x' if op in (0xe8,0xca) else 'y';setattr(self,reg,self.nz(getattr(self,reg)+(1 if op in (0xe8,0xc8) else -1),iw));return
        # Addressing for the regular ALU/load/store opcode families.
        modes={0x09:'imm',0x05:'dp',0x15:'dpx',0x0d:'abs',0x1d:'absx',0x19:'absy',0x0f:'long',0x1f:'longx',0x03:'sr',0x11:'indy',0x07:'indlong',0x17:'indlongy'}
        family=op&0xe0; form=op&0x1f
        mode=modes.get(form)
        is_index=op in (0xa2,0xae,0xa6,0xbe,0xb6,0xa0,0xac,0xa4,0xbc,0xb4,0xe0,0xec,0xe4,0xc0,0xcc,0xc4)
        extra={0x64:('stz','dp'),0x74:('stz','dpx'),0x9c:('stz','abs'),0x9e:('stz','absx'),0x86:('stx','dp'),0x8e:('stx','abs'),0x84:('sty','dp'),0x8c:('sty','abs'),0xe6:('inc','dp'),0xee:('inc','abs'),0xfe:('inc','absx'),0xc6:('dec','dp'),0xce:('dec','abs'),0xde:('dec','absx'),0x24:('bit','dp'),0x2c:('bit','abs'),0x89:('bit','imm'),0x04:('tsb','dp'),0x0c:('tsb','abs'),0x14:('trb','dp'),0x1c:('trb','abs')}
        shifts={0x0a:('asl',None),0x06:('asl','dp'),0x0e:('asl','abs'),0x1e:('asl','absx'),0x4a:('lsr',None),0x46:('lsr','dp'),0x2a:('rol',None),0x26:('rol','dp'),0x6a:('ror',None),0x7e:('ror','absx'),0x1a:('inc',None),0x3a:('dec',None)}
        if is_index:
            kind='ldx' if op&0xe0==0xa0 and op&2 else ('ldy' if op&0xe0==0xa0 else ('cpx' if op&0xe0==0xe0 else 'cpy'))
            mode={0:'imm',4:'dp',12:'abs',20:'dpx',22:'dpy',28:'absx',30:'absy'}.get(op&0x1c if op not in (0xbe,0xb6) else op&0x1e)
            if op in (0xa2,0xe0,0xc0,0xa0):mode='imm'
            width=iw
        elif op in extra: kind,mode=extra[op];width=iw if kind in ('stx','sty') else aw
        elif op in shifts:kind,mode=shifts[op];width=aw
        elif mode and family in (0,0x20,0x40,0x60,0x80,0xa0,0xc0,0xe0):kind={0:'ora',0x20:'and',0x40:'eor',0x60:'adc',0x80:'sta',0xa0:'lda',0xc0:'cmp',0xe0:'sbc'}[family];width=aw
        else:raise AssertionError(('unsupported opcode',hex(address),hex(op)))
        target=None
        if mode=='imm': value=self.fetch(width)
        else:
            if mode is None:value=self.a&mask
            else:
                if mode in ('dp','dpx','dpy'): target=(self.d+self.fetch()+(self.x if mode=='dpx' else self.y if mode=='dpy' else 0))&65535
                elif mode=='sr':target=(self.s+self.fetch())&65535
                elif mode in ('abs','absx','absy'):target=(self.db<<16)+self.fetch(2)+(self.x if mode=='absx' else self.y if mode=='absy' else 0)
                elif mode in ('long','longx'):target=self.fetch(3)+(self.x if mode=='longx' else 0)
                else:
                    pointer=(self.d+self.fetch())&65535
                    target=(self.read(pointer,2)+(self.db<<16)+self.y) if mode=='indy' else self.read(pointer,3)+(self.y if mode=='indlongy' else 0)
                value=self.read(target,width)
        bound=(1<<(width*8))-1; a=self.a&bound
        if kind in ('sta','stz','stx','sty'):self.write(target,{'sta':self.a,'stz':0,'stx':self.x,'sty':self.y}[kind],width);return
        if kind in ('lda','ldx','ldy'):
            value=self.nz(value,width)
            if kind=='lda':self.a=(self.a&0xff00|value) if width==1 else value
            else:setattr(self,'x' if kind=='ldx' else 'y',value)
            return
        if kind in ('cmp','cpx','cpy'):
            reg=a if kind=='cmp' else self.x if kind=='cpx' else self.y
            self.flag(1,reg>=value);self.nz(reg-value,width);return
        if kind=='bit':
            self.flag(2,not(a&value))
            if mode!='imm':self.flag(128,value&(1<<(width*8-1)));self.flag(64,value&(1<<(width*8-2)))
            return
        if kind in ('tsb','trb'):self.flag(2,not(a&value));self.write(target,value|a if kind=='tsb' else value&~a,width);return
        if kind in ('ora','and','eor'):result={'ora':a|value,'and':a&value,'eor':a^value}[kind]
        elif kind in ('adc','sbc'):
            operand=value if kind=='adc' else value^bound
            result=a+operand+(self.p&1);self.flag(1,result>bound)
            self.flag(64,(~(a^operand)&(a^result)&(1<<(width*8-1)))!=0)
        else:
            carry=self.p&1
            if kind in ('asl','rol'):result=(value<<1)+(carry if kind=='rol' else 0);self.flag(1,result>bound)
            elif kind in ('lsr','ror'):result=(value>>1)+(carry<<(width*8-1) if kind=='ror' else 0);self.flag(1,value&1)
            else:result=value+(1 if kind=='inc' else -1)
            result=self.nz(result,width)
            if target is not None:self.write(target,result,width);return
        result=self.nz(result,width)
        self.a=(self.a&0xff00|result) if width==1 else result
