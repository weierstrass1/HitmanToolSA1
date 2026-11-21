PlayerClusterSpriteInteraction:
    PHA
    SEP #$20
	STA !HitmanClusterContextIDLow,x
	XBA
	STA !HitmanClusterContextIDHigh,x

	LDA !HitmanSprite1Flip
	STA !HitmanClusterFlip,x

	JSL PlayerClippingRoutine

    JSL ClusterPositionSetup

    REP #$20
    PLA
    JSL ProcessPlayerHitBoxes
RTL
