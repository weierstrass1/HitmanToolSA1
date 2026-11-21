AnyNormalSpriteInteractionWithoutCallback:

    LDA !Hitman_Prop_HitboxSystem,x
    BNE +
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
    LDA $01,s
    TAX

    JSL NormalSpriteClippingRoutine

    LDX !SpriteIndex
    REP #$20
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxesWithoutCallback

    PLX
RTL

.OneHitboxInteraction
    LDA $01,s
    TAX

    JSL NormalSpriteOneHitbox2

    LDX !SpriteIndex
    REP #$20
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxesWithoutCallback

    PLX
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
    LDX !SpriteIndex
    REP #$20
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxesWithoutCallback
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
    LDA $01,s
    TAX

    JSL NormalSpriteLongOneHitbox2

    LDX !SpriteIndex
    REP #$20
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxesWithoutCallback

    PLX
RTL
