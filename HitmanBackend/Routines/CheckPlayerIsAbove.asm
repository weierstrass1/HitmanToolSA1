CheckPlayerIsAbove:
	LDA $65
	BPL .CheckAbove
RTL
.CheckAbove
	LDA !HitmanDefault
	BEQ +
RTL
+
	LDA !HitmanHBStatus
	ASL
	TAX

	REP #$20
	JSR (.CalculateDist,x)
	BCS +
	SEP #$20
	LDX !SpriteIndex
RTL
+
	PHA
	SEP #$20

	LDA $65
	BEQ .checkBox
	CMP #$07
	BNE .BoxWithPriority

.BoxWithoutPriority

	LDA !HitmanHBStatus
	CMP #$07
	BEQ .changeBox

	BRA .checkBox

.BoxWithPriority
	LDA !HitmanHBStatus
	CMP #$07
	BEQ .checkBox
.Return
	PLA
	PLA
	LDX !SpriteIndex
RTL	
.checkBox
	REP #$20
	LDA $68
	CMP $01,s
	SEP #$20
	BCS .changeBox
	BRA .Return

.changeBox

	LDA !HitmanHBStatus
	STA $65

	REP #$20
	LDA !HitmanHB1Top
	STA $66
	PLA
	STA $68
	SEP #$20
	LDX !SpriteIndex
RTL

.CalculateDist
	dw .Not
	dw .Not
	dw .Not
	dw .Not

	dw .Not
	dw .DownRight
	dw .DownLeft
	dw .Down

	dw .Not
	dw .UpRight
	dw .UpLeft
	dw .Up

	dw .Not
	dw .Right
	dw .Left
	dw .Not

.Not
	CLC
RTS

.DownRight
	LDA !HitmanHB1Top		;HB2.Top - HB1.Bottom
	SEC
	SBC !HitmanHB2Bottom
	PHA

	LDA !HitmanHB1Left		;HB2.Left - HB1.Right
	SEC
	SBC !HitmanHB2Right
	CMP $01,s
	BCS +

	STA $01,s

+
	PLA
	SEC
RTS

.DownLeft
	LDA !HitmanHB1Top		;HB2.Top - HB1.Bottom
	SEC
	SBC !HitmanHB2Bottom
	PHA

	LDA !HitmanHB2Left		;HB1.Left - HB2.Right
	SEC
	SBC !HitmanHB1Right
	CMP $01,s
	BCS +

	STA $01,s

+
	PLA
	SEC
RTS

.Down
	LDA !HitmanHB1Top		;HB2.Top - HB1.Bottom
	SEC
	SBC !HitmanHB2Bottom
	SEC
RTS

.UpRight
	LDA !HitmanHB2Top		;HB1.Top - HB2.Bottom
	SEC
	SBC !HitmanHB1Bottom
	PHA

	LDA !HitmanHB1Left		;HB2.Left - HB1.Right
	SEC
	SBC !HitmanHB2Right
	CMP $01,s
	BCS +

	STA $01,s

+
	PLA
	SEC
RTS

.UpLeft
	LDA !HitmanHB2Top		;HB1.Top - HB2.Bottom
	SEC
	SBC !HitmanHB1Bottom
	PHA

	LDA !HitmanHB2Left		;HB1.Left - HB2.Right
	SEC
	SBC !HitmanHB1Right
	CMP $01,s
	BCS +

	STA $01,s

+
	PLA
	SEC
RTS

.Up
	LDA !HitmanHB2Top		;HB1.Top - HB2.Bottom
	SEC
	SBC !HitmanHB1Bottom
	SEC
RTS

.Right
	LDA !HitmanHB1Left		;HB2.Left - HB1.Right
	SEC
	SBC !HitmanHB2Right
	SEC
RTS

.Left
	LDA !HitmanHB2Left		;HB1.Left - HB2.Right
	SEC
	SBC !HitmanHB1Right
	SEC
RTS
