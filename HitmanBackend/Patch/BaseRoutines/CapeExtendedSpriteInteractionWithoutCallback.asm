CapeExtendedSpriteInteractionWithoutCallback:
    STA !HitmanSprite1Context
    SEP #$20
	STA !HitmanExtendedContextIDLow,x
	XBA
	STA !HitmanExtendedContextIDHigh,x

	LDA !HitmanSprite1Flip
	STA !HitmanExtendedFlip,x

    LDA $13E8|!addr               ; If the cape is not spinning, return?.
    BEQ .NoContact
	LDA !HitmanExtendedPlayerIsAbove,x
	BEQ +
.NoContact
    CLC
RTL
+

	JSL CapeClipping

    JSL ExtendedPositionSetup

    REP #$20
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxesWithoutCallback
RTL
