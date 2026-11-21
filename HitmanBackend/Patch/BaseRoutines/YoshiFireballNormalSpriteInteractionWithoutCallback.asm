YoshiFireballNormalSpriteInteractionWithoutCallback:
	STA !HitmanSprite1Context
	SEP #$20
	STA !HitmanNormalContextIDLow,x
	XBA
	STA !HitmanNormalContextIDHigh,x

	LDA !HitmanSprite1Flip
	STA !HitmanNormalFlip,x

	JSL NormalSpritePositionSetup

	LDY #!MaxExtended-1
.Loop
	LDA !ExtendedNumber,y
    CMP #$11
	BEQ .Next

	JSL YoshiFireballClipping

    REP #$20
    PHY
    LDA !HitmanSprite1Context
    JSL ProcessHitBoxesWithoutCallback
	BCC .Next1
    PLY
RTL
.Next1
    PLY
.Next
    DEY
    BPL .Loop
    PLY
    CLC
RTL
