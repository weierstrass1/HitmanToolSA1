PlayerExtendedSpriteInteraction:
    PHA
    SEP #$20
	STA !HitmanExtendedContextIDLow,x
	XBA
	STA !HitmanExtendedContextIDHigh,x
	
	LDA !HitmanSprite1Flip
	STA !HitmanExtendedFlip,x

	JSL PlayerClippingRoutine

	JSL ExtendedPositionSetup

    REP #$20
    PLA
    JSL ProcessPlayerHitBoxes
RTL
