"""Isolated generation/Asar/CPU-contract verification; never publishes to input tools.

Run after dotnet build:
  python tests/verify_vanilla_interactions.py --rom <copy-source> --asar <asar.exe>
Use --sa1 when the source ROM already has SA-1 Pack installed.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

from cpu65816 import CPU


def verify(rom_path, asar, sa1=False):
    root = Path(__file__).resolve().parents[1]
    backend = root / 'HitmanBackend'
    artifacts = root / 'artifacts/vanilla-interactions'
    artifacts.mkdir(parents=True, exist_ok=True)
    stage = Path(tempfile.mkdtemp(prefix='sa1-' if sa1 else 'lorom-', dir=artifacts))
    for directory in ('ASM', 'Patch', 'Routines', 'HitboxInfo', 'CustomProperties', 'PixiRoutines'):
        shutil.copytree(backend / directory, stage / directory, dirs_exist_ok=True)
    for directory in ('CustomProperties/PerSprite', 'Pixi/asm/ExtraDefines', 'Pixi/routines/Spawn', 'Json'):
        (stage / directory).mkdir(parents=True, exist_ok=True)
    (stage / 'list.txt').write_text('F0 Fixture.json\n')
    (stage / 'CustomProperties/PerSprite/Fixture.json').write_text(json.dumps({
        'Name': 'Fixture', 'IsCustom': True, 'VanillaInteractionCallback': 'FixtureEvent',
        'Properties': [{'PropertyName': 'HitboxSystem', 'Value': '03'},
                       {'PropertyName': 'VanillaEventMask', 'Value': '7F'}]}))
    (stage / 'Routines/FixtureEvent.asm').write_text('''FixtureEvent:
    STA $65
    INC $66
    LDA !SpriteStatus,x
    STA $68
    LDA !Hitman_Prop_ActionFlag,x
    STA $69
    LDA #$AA
    STA $0E
    LDA $67
    BEQ .Allow
    SEC
    RTL
.Allow
    CLC
    RTL
''')
    (stage / 'HitboxInfo/Fixture.hitboxinfo').write_text('''PoseHitBoxes:
 db $00,$01,$FF
 db $02,$FF
 db $FF
HitboxXOffset:
 dw $FFFE,$0028,$0050
HitboxXOffsetFlip:
 dw $0004,$FFD8,$FFB0
HitboxYOffset:
 dw $0000,$0000,$0000
HitboxWidth:
 dw $0010,$0008,$0008
HitboxHeight:
 dw $0010,$0010,$0010
HitboxAction1:
 dw $0000,$0000,$0000
HitboxAction2:
 dw $0000,$0000,$0000
Actions:
 dw Nothing
''')
    original = rom_path.read_bytes()
    digest = hashlib.sha256(original).hexdigest()
    header = 512 if len(original) % 0x8000 == 512 else 0
    assert (original[header+0x7fd5] == 0x23) == sa1
    data = original + bytes(max(0, 0x200000 + header - len(original)))
    (stage / 'input.smc').write_bytes(data)
    (stage / 'output.smc').write_bytes(data)
    (stage / 'Json/Settings.json').write_text(json.dumps({
        'InputPath': 'input.smc', 'OutputPath': 'output.smc',
        'PixiFolder': 'Pixi', 'PixiList': 'list.txt'}))
    # DX's exported SMW table symbols live in DyzenDefines.asm in this version.
    dx = root.parent / 'Dynamic-X/DynamicXtremeLibrary/ExtraDefines/DyzenDefines.asm'
    shutil.copyfile(dx, stage / 'Pixi/asm/ExtraDefines/DyzenDefines.asm')
    env = dict(os.environ, HITMAN_ASAR_EXE=str(asar))
    tool = root / 'HitmanTool/bin/Debug/net8.0/HitmanTool.dll'
    result = subprocess.run(['dotnet', str(tool), '-use-settings'], cwd=stage,
                            env=env, capture_output=True, text=True)
    (stage / 'run.log').write_text(result.stdout + '\n' + result.stderr, encoding='utf-8')
    assert result.returncode == 0, result.stderr
    tables = (stage / 'Patch/Files/CustomPropertiesTables.asm').read_text()
    for name, body in re.findall(r'(CustomProperty_\w+):\s*(.*?)(?=CustomProperty_|\Z)', tables, re.S):
        values = re.findall(r'\$([0-9A-Fa-f]{2})\b', body)
        assert len(values) == 256, (name, len(values))
        if name == 'CustomProperty_Custom_VanillaCallbackID':
            assert values[0xf0] == '01'
    # Assemble a diagnostic copy as well, obtaining labels and resolved addresses.
    defines = re.findall(r'^\s*!(\w+)\s*=', (stage/'ASM/HitmanDefines.asm').read_text(), re.M)
    defines += re.findall(r'^\s*!(\w+)\s*=', (stage/'Patch/Files/HitmanDefines.asm').read_text(), re.M)
    defines += re.findall(r'^\s*!(\w+)\s*=', (stage/'ASM/VanillaInteractionDefines.asm').read_text(), re.M)
    defines += ['SpriteXLow','SpriteXHigh','SpriteYLow','SpriteYHigh','SpriteStatus',
                'SpriteIndex','SpriteDirection','SpriteDecTimer5','SpriteBlockedStatus_ASB0UDLR']
    defines += ['ExtraBits','CustomSpriteNumber']
    defines += ['ExtendedNumber','ExtendedMiscTable2','ExtendedXLow','ExtendedXHigh','ExtendedYLow','ExtendedYHigh',
                'SpriteTweaker190F_WCDJ5SDP','SpriteTweaker166E_LWCFPPPG','SpriteMiscTable7','SpriteYSpeed']
    defines = [n for n in defines if n != 'VanillaInteractions' and not n.startswith(('Hitman_Base_', 'Hitman_Callback_'))]
    diagnostic = 'incsrc "Patch/Hitman.asm"\n' + '\n'.join(
        f'print "RAM_{name}=", hex(!{name})' for name in sorted(set(defines)))
    diagnostic += '\n' + '\n'.join(f'!Use{name}Interaction = 1' for name in
        ['CapeStomping','YoshiStomping','Cape','YoshiFireball','Fireball','Bobomb','Sprite'])
    diagnostic += '\n%VanillaInteractionFlag()\nprint "RAM_LegacyFlags=", hex(!VanillaInteractions)\n'
    (stage / 'verify.asm').write_text(diagnostic)
    shutil.copyfile(stage/'input.smc', stage/'verify.smc')
    result = subprocess.run([str(asar), '--no-title-check', '--symbols=wla',
                             '--symbols-path=symbols.sym', 'verify.asm', 'verify.smc'],
                            cwd=stage, capture_output=True, text=True)
    (stage / 'asar.log').write_text(result.stdout+'\n'+result.stderr, encoding='utf-8')
    assert result.returncode == 0, result.stderr
    ram = {n: int(v,16) for n,v in re.findall(r'RAM_(\w+)=([0-9A-Fa-f]+)', result.stdout)}
    labels = {n: int(b+a,16) for b,a,n in re.findall(r'^([0-9A-Fa-f]{2}):([0-9A-Fa-f]{4}) (\w+)$',
               (stage/'symbols.sym').read_text(), re.M)}
    patched = (stage/'verify.smc').read_bytes()[header:]
    assert ram['HitmanRAMEnd']-ram['HitmanRAMBase']==(652 if sa1 else 492)
    assert ram['LegacyFlags']==0x7f
    passed = []

    def cpu():
        c = CPU(patched, labels, sa1)
        c.db = 2
        c.a = 0xa500
        return c

    def put(c, name, slot, value):
        c.write(ram[name]+slot, value)

    def position(c, slot, x, y):
        for axis,value in [('X',x),('Y',y)]:
            put(c,'Sprite'+axis+'Low',slot,value)
            put(c,'Sprite'+axis+'High',slot,value>>8)

    def box(c, slot, system, x=100, y=80):
        position(c,slot,x,y)
        put(c,'Hitman_Prop_HitboxSystem',slot,system)
        for key,value in [('WidthLow',16),('HeightLow',16),('XOffLow',0),('YOffLow',0)]:
            put(c,'HitmanNormal'+key,slot,value)
        if system==3:
            context=ram['FixtureHitboxContext']
            put(c,'HitmanNormalContextIDLow',slot,context)
            put(c,'HitmanNormalContextIDHigh',slot,context>>8)

    def events(c, slot, mask=0x7f, response=0x7f, callback=1):
        for name,v in [('VanillaEventMask',mask),('VanillaResponseMask',response),('VanillaCallbackID',callback)]:
            put(c,'Hitman_Prop_'+name,slot,v)

    for event in (1,2,4,8,16,32,64):
        for masked, callback in [(False,0),(False,1),(True,1)]:
            c=cpu();box(c,3,2);events(c,3,mask=0 if masked else 127,callback=callback)
            c.a=0xa500|event;c.x=3;c.y=0x55
            c.write(c.d+0x0e,0x35);c.write(c.d+0x67,1)
            c.run('Hitman_Base_DispatchVanillaCallback')
            assert (c.x,c.y,c.db,c.p&0x30,c.read(c.d+0x0e))==(3,0x55,2,0x30,0x35)
            assert c.read(c.d+0x66)==int(bool(callback and not masked))
            assert c.read(ram['Hitman_Prop_ActionFlag']+3)==0
    passed.append('callback-only dispatch preserves the caller and leaves ActionFlag to the hook')

    c=cpu();c.x=5;c.db=labels['CustomPropertiesSetup']>>16
    c.write(ram['ExtraBits']+5,8);c.write(ram['CustomSpriteNumber']+5,0xf0)
    c.write(ram['Hitman_Prop_ActionFlag']+5,0x7f)
    c.run('CustomPropertiesSetup',short=True)
    assert c.read(ram['Hitman_Prop_HitboxSystem']+5)==3
    assert c.read(ram['Hitman_Prop_VanillaCallbackID']+5)==1
    assert c.read(ram['Hitman_Prop_ActionFlag']+5)==0
    c.db=2;c.y=7;c.write(ram['Hitman_Prop_ActionFlag']+5,0x7f)
    c.run('HitmanRefreshCustomProperties')
    assert c.read(ram['Hitman_Prop_ActionFlag']+5)==0 and (c.db,c.x,c.y)==(2,5,7)
    passed.append('F0 custom setup selects callback and clears stale flags')

    for system in (0,1,2,3,4):
        c=cpu();box(c,3,system);c.x=3;c.y=0;c.p=0x30
        c.run('Hitman_Base_LoadNormalHitbox')
        assert bool(c.p&1)==(system!=0),(system,c.trace[-5:])
        assert (c.x,c.y,c.db,c.p&0x30)==(3,0,2,0x30)
        if system in (2,3,4):
            assert c.read(c.d+0x45,2)==100+256+(-2 if system==3 else 0)
        if system==3:
            c.y=1;c.run('Hitman_Base_LoadNormalHitbox')
            assert c.p&1 and c.read(c.d+0x45,2)==396
            c.y=2;c.run('Hitman_Base_LoadNormalHitbox');assert not c.p&1
    passed.append('all five hitbox systems and multi-box ordinal/terminator')

    for a in range(5):
        for b in range(5):
            c=cpu();box(c,1,a);box(c,2,b);c.x=1;c.y=2
            c.run('Hitman_Base_CheckNormalPair')
            assert bool(c.p&1)==(a!=0 and b!=0),(a,b,hex(c.pc))
            assert (c.x,c.y,c.db,c.p&0x30)==(1,2,2,0x30)
    passed.append('25 pair combinations, executing vanilla clipping too')

    c=cpu();box(c,1,3);box(c,2,2,x=140);c.x=1;c.y=2
    c.run('Hitman_Base_CheckNormalPair');assert c.p&1
    position(c,2,200,80);c.run('Hitman_Base_CheckNormalPair');assert not c.p&1
    passed.append('second-box contact and separated pair')

    c=cpu();box(c,3,3);c.x=3;c.y=0;c.p=0x30
    put(c,'HitmanNormalContextIDLow',3,ram['FixtureHitboxContext']+1)
    c.run('Hitman_Base_LoadNormalHitbox');assert c.p&1 and c.read(c.d+0x45,2)==436
    put(c,'HitmanNormalContextIDLow',3,ram['FixtureHitboxContext']+2)
    c.run('Hitman_Base_LoadNormalHitbox');assert not c.p&1
    put(c,'HitmanNormalContextIDLow',3,ram['FixtureHitboxContext'])
    put(c,'HitmanNormalFlip',3,1)
    c.run('Hitman_Base_LoadNormalHitbox');assert c.read(c.d+0x45,2)==360
    passed.append('distinct pose, empty pose and horizontal flip offsets')

    c=cpu();box(c,3,3);c.x=3;c.y=1;c.a=ram['FixtureHitboxContext'];c.p=0
    c.write(c.d+0x8a,356,2);c.write(c.d+0x8c,336,2)
    c.run('Hitman_Base_GetHitbox2');assert c.p&1 and c.read(c.d,2)==396
    c=cpu();box(c,3,3);c.x=3
    for offset,value in [(0x45,390),(0x47,336),(0x49,400),(0x4b,352)]:c.write(c.d+offset,value,2)
    c.run('Hitman_Base_AnyNormalSpriteInteractionOneHitbox')
    assert c.p&1 and c.x==3
    passed.append('legacy GetHitbox2/OneHitbox multi-box ABI and balanced stack')

    # Actual patched driver entries, with callbacks consuming the response.
    for status,event in [(8,2),(10,1)]:
        c=cpu();box(c,0,3);box(c,1,3)
        put(c,'SpriteStatus',0,status);put(c,'SpriteStatus',1,8)
        events(c,0,response=0);events(c,1,response=0);c.write(ram['SpriteIndex'],1);c.write(c.d+0x67,1)
        c.x=1;c.y=0;c.run('Hitman_Base_RunVanillaSpriteInteraction')
        assert c.read(ram['Hitman_Prop_ActionFlag']+1)==event,(status,event)
        assert c.read(c.d+0x66)==(2 if event==2 else 1)
    passed.append('patched regular/shell drivers, no duplicate per-hitbox events')

    c=cpu();box(c,1,2);events(c,1);put(c,'SpriteStatus',1,8)
    put(c,'SpriteBlockedStatus_ASB0UDLR',1,4);put(c,'Hitman_Prop_VanillaResponseMask',1,0);c.write(c.d+0x67,1)
    c.run(0x0294c1)
    assert c.read(ram['Hitman_Prop_ActionFlag']+1)==64 and c.read(c.d+0x66)==1
    assert c.read(0x1887+(0x6000 if sa1 else 0))==0x30
    passed.append('global cape dive, callback, shake, long-return stack')

    for kind,event in [(5,8),(0x11,16)]:
        c=cpu();box(c,1,3);events(c,1);put(c,'SpriteStatus',1,8)
        for name,value in [('Number',kind),('XLow',100),('XHigh',0),('YLow',80),('YHigh',0)]:
            put(c,'Extended'+name,8,value)
        put(c,"Hitman_Prop_VanillaResponseMask",1,0);c.write(c.d+0x67,1);c.x=8
        c.run('Hitman_Base_RunVanillaFireballInteraction')
        assert c.read(ram['Hitman_Prop_ActionFlag']+1)==event
        assert c.read(c.d+0x66)==1 and c.x==8
    passed.append('patched fireball/Yoshi drivers and extended index restoration')

    c=cpu();box(c,1,3);box(c,2,3);events(c,1)
    put(c,'SpriteStatus',1,8);put(c,'SpriteStatus',2,8)
    c.write(ram['SpriteIndex'],2);put(c,"Hitman_Prop_VanillaResponseMask",1,0);c.write(c.d+0x67,1);c.x=2
    c.run(0x028139,short=True,bank=2 if sa1 else 0x82)
    assert c.read(ram['Hitman_Prop_ActionFlag']+1)==4 and c.read(c.d+0x66)==1 and c.x==2
    passed.append('patched explosion loop, source excluded, one victim callback')

    def quake(c):
        box(c,1,3);events(c,1);put(c,'SpriteStatus',1,8)
        for name,value in [('Index',0),('Status',1),('Timer',3),('XLow',100),('XHigh',0),('YLow',80),('YHigh',0)]:
            c.write(ram['HitmanQuake'+name],value)

    c=cpu();quake(c);put(c,"Hitman_Prop_VanillaResponseMask",1,0);c.write(c.d+0x67,1);c.x=0
    c.run('Hitman_Base_RunVanillaQuakeInteraction')
    assert c.read(ram['Hitman_Prop_ActionFlag']+1)==32 and c.read(c.d+0x66)==1 and c.x==0
    passed.append('original quake activation timer through rewritten loop')

    c=cpu();quake(c);put(c,'Hitman_Prop_VanillaCallbackID',1,0)
    rom_bank=0 if sa1 else 0x800000
    for routine in (0x01ab6f,0x02ace5):c.stubs[routine|rom_bank]=lambda c:None
    c.x=0;c.run('Hitman_Base_RunVanillaQuakeInteraction')
    assert c.read(ram['Hitman_Prop_ActionFlag']+1)==32
    assert c.read(ram['SpriteStatus']+1)==2 and c.read(ram['SpriteYSpeed']+1)==0xc0
    passed.append('rewritten quake default kill/speed; game SFX/score calls stubbed')

    c=cpu();box(c,0,3);box(c,1,3);events(c,0);events(c,1)
    put(c,'SpriteStatus',0,8);put(c,'SpriteStatus',1,8);c.write(ram['SpriteIndex'],1);c.x=1
    for _ in range(2):c.run(0x01a411,short=True,bank=1 if sa1 else 0x81)
    assert c.read(c.d+0x66)==4
    assert c.read(ram['Hitman_Prop_ActionFlag'])==2 and c.read(ram['Hitman_Prop_ActionFlag']+1)==2
    passed.append('sticky regular flags do not suppress later callbacks')

    for current,other,victim in [(8,10,1),(10,8,0)]:
        c=cpu();box(c,0,3);box(c,1,3);events(c,0);events(c,1)
        put(c,'SpriteStatus',0,other);put(c,'SpriteStatus',1,current)
        c.write(ram['SpriteIndex'],1);c.write(c.d+0x67,1);c.x=1
        for routine in (0x01ab72,0x02ace1,0x02ace5):c.stubs[routine|rom_bank]=lambda c:None
        c.run(0x01a411,short=True,bank=1 if sa1 else 0x81)
        assert c.read(ram['SpriteStatus']+victim)==2
        assert c.read(ram['Hitman_Prop_ActionFlag']+victim)==1
        assert (c.read(c.d+0x68),c.read(c.d+0x69))==(2,1)
    passed.append('shell callback sees final status and ActionFlag in both slot orientations, even with SEC')

    for kind,event in [(5,8),(0x11,16)]:
        c=cpu();box(c,1,3);events(c,1);put(c,'SpriteStatus',1,8)
        put(c,'SpriteTweaker190F_WCDJ5SDP',1,8)
        for name,value in [('Number',kind),('XLow',100),('XHigh',0),('YLow',80),('YHigh',0)]:
            put(c,'Extended'+name,8,value)
        c.x=8;c.run('Hitman_Base_RunVanillaFireballInteraction')
        assert c.read(ram['Hitman_Prop_ActionFlag']+1)==event
        assert c.read(ram['SpriteMiscTable7']+1)==1
        assert c.read(ram['ExtendedNumber']+8)==(1 if kind==5 else 0x11)
        assert c.read(ram['ExtendedMiscTable2']+8)==(15 if kind==5 else 0)
    passed.append('default fire counter, smoke-state consumption and persistent Yoshi fire')

    for current in (8,9,10,11):
        for other in (8,9,10,11):
            c=cpu();box(c,0,3);box(c,1,3);events(c,0,response=0);events(c,1,response=0)
            put(c,'SpriteStatus',0,other);put(c,'SpriteStatus',1,current)
            c.write(ram['SpriteIndex'],1);c.write(c.d+0x67,1);c.x=1
            c.run(0x01a411,short=True,bank=1 if sa1 else 0x81)
            assert c.read(ram['SpriteStatus'])==other and c.read(ram['SpriteStatus']+1)==current
    passed.append('all 16 normal/item/kicked/carried pairs preserve stack on consumed events')

    # Asar failure must not publish the intermediate ROM or generated defines.
    output_hash=hashlib.sha256((stage/'output.smc').read_bytes()).hexdigest()
    exported=stage/'Pixi/asm/ExtraDefines/HitmanDefines.asm'
    export_hash=hashlib.sha256(exported.read_bytes()).hexdigest()
    callback=stage/'Routines/FixtureEvent.asm';valid_callback=callback.read_text()
    callback.write_text('error "intentional verification failure"\n')
    failed=subprocess.run(['dotnet',str(tool),'-use-settings'],cwd=stage,env=env,capture_output=True,text=True)
    (stage/'expected-failure.log').write_text(failed.stdout+'\n'+failed.stderr,encoding='utf-8')
    assert failed.returncode!=0
    assert hashlib.sha256((stage/'output.smc').read_bytes()).hexdigest()==output_hash
    assert hashlib.sha256(exported.read_bytes()).hexdigest()==export_hash
    callback.write_text(valid_callback)
    (stage/'Patch/Routines/FixtureEvent.asm').write_text(valid_callback)
    passed.append('failed Asar assembly leaves published ROM/defines unchanged')

    assert hashlib.sha256(rom_path.read_bytes()).hexdigest()==digest
    report={'mapper':'SA-1' if sa1 else 'LoROM','checks':passed,
            'input_unchanged':True,'gameplay_or_timing_tested':False}
    (stage/'verification.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--rom',type=Path,required=True)
    parser.add_argument('--asar',type=Path,required=True)
    parser.add_argument('--sa1',action='store_true')
    args=parser.parse_args()
    verify(args.rom,args.asar,args.sa1)
