CheckBounce:	
	; Keep the normal surface already selected for this check.
	LDA !HitmanNormalPlayerIsAbove,x
	BMI .SaveContactBox
	BEQ .SaveContactBox
	BIT #$40
	BNE .SaveContactBox
	REP #$20
	LDA $6A
	CMP #$FFFF
	SEP #$20
	BEQ .SaveContactBox
	LDA #$08
	STA !HitmanNormalPlayerIsAbove,x
	LDA #$01
	TSB !HitmanDefault
RTL
.SaveContactBox
	; Contact effects use these bounds even when Mario does not bounce.
	REP #$20
	LDA !HitmanHB1Top
	STA $6A
	LDA !HitmanHB1Left
	STA $6C
	LDA !HitmanHB1Right
	STA $6E
	LDA !HitmanHB1Bottom
	STA $1487|!addr
	SEP #$20
	LDA $65
	BPL .ProcessBounce
	LDA #$48
	STA !HitmanNormalPlayerIsAbove,x
RTL
.ProcessBounce
	LDA #$01
	TSB !HitmanDefault
	
	LDA !HitmanNormalPlayerIsAbove,x
	BMI .CheckDistance
	BEQ .CheckDistance
	BIT #$40
	BNE .CheckDistance
	LDA #$08
	JMP .AcceptBounce
.CheckDistance
	REP #$20
	LDA !HitmanHB1Top
	SEC
	SBC !HitmanHB2Bottom
	BPL +
	EOR #$FFFF
	INC A
+
	CMP #$0005
	BCS .CheckTolerance
	SEP #$20
	LDA #$08
	JMP .AcceptBounce
.CheckTolerance
	LDA !HitmanHB1Bottom
	CMP !HitmanHB2Bottom
	SEP #$20
	BCS .CheckSpeeds
RTL
.CheckSpeeds
	LDA #$00
	PHA
	LDA !SpriteYSpeed,x
	PHA
	LDA !SpriteBlockedStatus_ASB0UDLR,x
	AND #$24
	BEQ +
	LDA #$00
	STA $01,s

	LDA #$01
	STA $02,s
+
	LDA !PlayerYSpeed
	PHA
	LDA !PlayerBlockedStatus_S00MUDLR
	AND #$04
	BEQ +
	LDA #$00
	STA $01,s

	LDA $03,s
	INC A
	STA $03,s
+
	LDA $01,s
	BMI +
	LDA $02,s
	BMI .ValidHit
+
	LDA $02,s
	SEC
	SBC $01,s
	BMI .ValidHit
	PLA
	PLA
	PLA
RTL
.ValidHit
	PLA
	PLA
	PLA
	CMP #$02
	BCC +
RTL
+
	LDA #$48
	JMP .AcceptBounce
.AcceptBounce
	STA !HitmanNormalPlayerIsAbove,x
RTL
