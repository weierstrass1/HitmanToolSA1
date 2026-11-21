; M=X=1, X=receiver, Y=other normal slot. Checks both configured systems.
; Exit M=X=1, C=contact. Preserves X/Y, DBR/D/stack; scratch clobbered.
CheckNormalPair:
    LDA !Hitman_Prop_HitboxSystem,x
    BEQ .Disabled
    PHX
    TYX
    LDA !Hitman_Prop_HitboxSystem,x
    PLX
    CMP #$00
    BEQ .Disabled
    PHX
    PHY
    LDY #$00
.SourceLoop
    LDA $01,s
    TAX
    JSL LoadNormalHitbox
    BCC .NoContact
    PHY
    LDA $03,s
    TAX
    JSL AnyNormalSpriteInteractionOneHitbox
    PLY
    BCS .Contact
    INY
    BNE .SourceLoop
.NoContact
    PLY
    PLX
    CLC
    RTL
.Contact
    PLY
    PLX
    SEC
    RTL
.Disabled
    CLC
    RTL
