; Complete GroundPound rewrite. Long entry; no local Mario hitbox involved.
; M=X=1. Preserves DBR/D/stack. Original routine clobbers X/Y and SpriteIndex.
org $0294C1|!rom
    autoclean JML HitmanCapeDiveInteraction
    NOP

freecode
HitmanCapeDiveInteraction:
    LDA #$30
    STA !HitmanGroundShake
    STZ !HitmanCapeGroundTimer
    PHB
    PHK
    PLB
    LDX.b #!MaxSprites-3
.Loop
    STX !SpriteIndex
    LDA !SpriteStatus,x
    CMP #$08
    BCC .Next
    LDA !SpriteBlockedStatus_ASB0UDLR,x
    AND #$04
    BEQ .Next
    LDA !SpriteTweaker166E_LWCFPPPG,x
    AND #$20
    ORA !SpriteYoshiTongueFlag,x
    ORA !SpriteDecTimer2,x
    BNE .Next
    LDA #$35
    STA $0E
    LDA !Hitman_Prop_HitboxSystem,x
    BEQ .Next
    LDA !Hitman_Prop_VanillaResponseMask,x
    AND #!HitmanEventCapeDive
    BEQ .Event
    JSL HitmanQuakeDefaultResponseLong
.Event
    %HitmanMarkAction(!HitmanEventCapeDive)
    LDA #!HitmanEventCapeDive
    LDY #$FF
    JSL Hitman_Base_DispatchVanillaCallback
.Next
    DEX
    BPL .Loop
    PLB
    RTL
