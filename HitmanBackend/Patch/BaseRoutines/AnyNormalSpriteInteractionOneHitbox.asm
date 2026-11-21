AnyNormalSpriteInteractionOneHitbox:

    LDA !Hitman_Prop_HitboxSystem,x
    CMP #$05
    BCS .Disabled
    CMP #$00
    BNE +
.Disabled
    CLC
RTL
+
    PHX

    ASL
    TAX

    JMP (.System,x)

.System
    dw .NoInteraction
    dw .VanillaInteraction
    dw .OneHitboxInteraction
    dw .HitmanInteraction
    dw .OneLongHitboxInteraction

.NoInteraction
    PLX
    CLC
RTL

.VanillaInteraction
    PLX

    JSL NormalSpriteClippingRoutine

    JSL CheckContact
RTL

.OneHitboxInteraction
    PLX

    JSL NormalSpriteOneHitbox2

    JSL CheckContact
RTL

.HitmanInteraction
    LDA $01,s
    TAX

    JSL NormalSpritePositionSetup2

    LDA !HitmanNormalFlip,x
    STA !HitmanSprite2Flip

    LDA !HitmanNormalContextIDHigh,x
    STA !HitmanSprite2Context+1
    LDA !HitmanNormalContextIDLow,x
    STA !HitmanSprite2Context

    LDY #$00
.Loop
    PHY

    REP #$30
    LDA !HitmanSprite2Context
    JSL GetHitbox2
    BCS .ValidHitbox
    PLY
    PLX
RTL

.ValidHitbox
    JSL CheckContact
    BCC .next

    PLA
    PLX
    SEC
RTL

.next
    PLY
    INY
    BNE .Loop 

    PLX
    CLC
RTL

.OneLongHitboxInteraction
    PLX

    JSL NormalSpriteLongOneHitbox2

    JSL CheckContact
RTL
