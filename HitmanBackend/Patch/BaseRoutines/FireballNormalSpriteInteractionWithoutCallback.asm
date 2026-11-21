FireballNormalSpriteInteractionWithoutCallback:
	STA !HitmanSprite1Context
	SEP #$20
	STA !HitmanNormalContextIDLow,x
	XBA
	STA !HitmanNormalContextIDHigh,x

	LDA !HitmanSprite1Flip
	STA !HitmanNormalFlip,x

	JSL NormalSpritePositionSetup

.Slot0
	LDY #!MaxExtended-1
	LDA !ExtendedNumber,y
	BEQ .Slot1

	JSL FireballClipping

    REP #$20
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxesWithoutCallback
	BCC .Slot1
	LDY #!MaxExtended-1
RTL
.Slot1
	LDY #!MaxExtended-2
	LDA !ExtendedNumber,y
	BEQ +

	JSL FireballClipping

    REP #$20
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxesWithoutCallback
	LDY #!MaxExtended-2
RTL
+
	CLC
RTL
