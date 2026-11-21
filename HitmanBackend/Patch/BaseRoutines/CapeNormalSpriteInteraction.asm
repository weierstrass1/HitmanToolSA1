CapeNormalSpriteInteraction:
    STA !HitmanSprite1Context
    SEP #$20
	STA !HitmanNormalContextIDLow,x
	XBA
	STA !HitmanNormalContextIDHigh,x

	LDA !HitmanSprite1Flip
	STA !HitmanNormalFlip,x

    LDA $13E8|!addr               ; If the cape is not spinning, return?.
    BEQ .NoContact
    LDA !SpriteBeingEaten,x       ; If the sprite can't contact the cape,
    BNE .NoContact
    LDA !SpriteBehindScenery,x    ; If the player and the sprite are on
    PHY                           ; different sides of the net, return?.
    LDY $74
    BEQ +
    EOR #$01
+
    PLY
    EOR $13F9|!addr
    BNE .NoContact
	LDA !HitmanNormalPlayerIsAbove,x
	BEQ +
.NoContact
    CLC
RTL
+

	JSL CapeClipping

    JSL NormalSpritePositionSetup

    REP #$20
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxes
RTL
