CapeClusterSpriteInteraction:
    STA !HitmanSprite1Context
    SEP #$20
	STA !HitmanClusterContextIDLow,x
	XBA
	STA !HitmanClusterContextIDHigh,x

	LDA !HitmanSprite1Flip
	STA !HitmanClusterFlip,x

    LDA $13E8|!addr               ; If the cape is not spinning, return?.
    BEQ .NoContact
	LDA !HitmanClusterPlayerIsAbove,x
	BEQ +
.NoContact
    CLC
RTL
+

	JSL CapeClipping

    JSL ClusterPositionSetup

    REP #$20
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxes
RTL
