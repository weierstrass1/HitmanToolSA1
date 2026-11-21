CheckBounceSurface:	

	LDA #$01
	TSB !HitmanDefault

	REP #$20

	LDA !HitmanHB1Top
	CMP $6A
	BCS +
	STA $6A

	LDA !HitmanHB1Left
	STA $6C
	LDA !HitmanHB1Right
	STA $6E
	LDA !HitmanHB1Bottom
	STA $1487|!addr

+
	SEP #$20
	
	LDA !HitmanNormalPlayerIsAbove,x
	BMI +
	BEQ +
	LDA #$08
	STA !HitmanNormalPlayerIsAbove,x
RTL
+
	REP #$20
	LDA !HitmanHB1Top
	SEC
	SBC !HitmanHB2Bottom
	BPL +
	EOR #$FFFF
	INC A
+
	CMP #$0005
	SEP #$20
	BCS +
	LDA #$08
	STA !HitmanNormalPlayerIsAbove,x
+
RTL