; Full explosion-vs-normal loop. Original JSR entry: X=exploding normal slot.
; Returns through the original bank-02 RTS; M=X=1, DBR/D/stack preserved.
org $028139|!rom
    autoclean JML HitmanExplosionInteraction
    NOP

freecode
HitmanExplosionInteraction:
    PHB
    PHK
    PLB
    PHX
    LDY.b #!MaxSprites-3
.Loop
    CPY !SpriteIndex
    BEQ .Next
    LDA !SpriteStatus,y
    CMP #$08
    BCC .Next
    LDA !SpriteTweaker167A_DPMKSPIS,y
    AND #$02
    BNE .Next
    PHY
    TYX
    LDY !SpriteIndex
    JSL Hitman_Base_CheckNormalPair
    PLY
    BCC .RestoreSource
    PHY
    TYX
    LDA !Hitman_Prop_VanillaResponseMask,x
    AND #!HitmanEventExplosion
    BEQ .Event
    LDA #$02
    STA !SpriteStatus,x
    LDA #$C0
    STA !SpriteYSpeed,x
    STZ !SpriteXSpeed,x
.Event
    %HitmanMarkAction(!HitmanEventExplosion)
    LDY !SpriteIndex
    LDA #!HitmanEventExplosion
    JSL Hitman_Base_DispatchVanillaCallback
    PLY
.RestoreSource
    LDX !SpriteIndex
.Next
    DEY
    BPL .Loop
    PLX
    PLB
    JML $02814F|!rom
