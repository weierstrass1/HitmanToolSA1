; A=0 disables, A=1 enables; X and Y are normal sprite slots.
; M=X=1. Preserves X/Y/DBR/D, uses $00-$01. Invalid slots and self-pairs are ignored.
SetSpritePairCollision:
    CPX.b #!MaxSprites
    BCS .Return
    CPY.b #!MaxSprites
    BCS .Return
    PHX
    PHY
    AND #$01
    BEQ .State
    LDA #$FF
.State
    STA $01
    STX $00
    TYA
    CMP $00
    BEQ .Restore
    BCC .XHigher
    TYX
    BRA .Index
.XHigher
    STY $00
.Index
    LDA.l HitmanPairRowBase,x
    LDX $00
    %HitmanWritePairBit()
.Restore
    PLY
    PLX
.Return
    RTL
