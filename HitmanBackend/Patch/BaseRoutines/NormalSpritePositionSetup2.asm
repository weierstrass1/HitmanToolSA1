NormalSpritePositionSetup2:
	LDA !SpriteXHigh,x
	STA !HitmanSprite2X+1
	LDA !SpriteXLow,x
	STA !HitmanSprite2X

	LDA !SpriteYHigh,x
	STA !HitmanSprite2Y+1
	LDA !SpriteYLow,x
	STA !HitmanSprite2Y

	REP #$21
	LDA !HitmanSprite2X
	ADC #$0100
	STA !HitmanSprite2X

	LDA !HitmanSprite2Y
	CLC
	ADC #$0100
	STA !HitmanSprite2Y
	SEP #$20
RTL