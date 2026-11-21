ExtendedPositionSetup:
	LDA !ExtendedXHigh,x
	STA !HitmanSprite1X+1
	LDA !ExtendedXLow,x
	STA !HitmanSprite1X

	LDA !ExtendedYHigh,x
	STA !HitmanSprite1Y+1
	LDA !ExtendedYLow,x
	STA !HitmanSprite1Y

	REP #$21
	LDA !HitmanSprite1X
	ADC #$0100
	STA !HitmanSprite1X

	LDA !HitmanSprite1Y
	CLC
	ADC #$0100
	STA !HitmanSprite1Y
	SEP #$20
RTL