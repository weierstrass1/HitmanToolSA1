PlayerFloor:
    LDA !PlayerYSpeed
    BPL +
    CLC
RTL
+
    REP #$20
    LDA !HitmanHB1Top
    SEC
    SBC !HitmanHB2Bottom
    BEQ .Start
    BCC +
    SEP #$20
    CLC
RTL
+
    CMP #$FFF8
    BCS .Start
    SEP #$20
    CLC
RTL
.Start
    PHA
    CLC
    ADC !PlayerY
    INC A
    STA !PlayerY

    LDA $01,s
    CLC
    ADC !HitmanHB2Top
    STA !HitmanHB2Top
    PLA
    CLC
    ADC !HitmanHB2Bottom
    STA !HitmanHB2Bottom
    SEP #$20
    
    LDA #$04
    TSB !PlayerBlockedStatus_S00MUDLR
    STZ !PlayerYSpeed

	LDA #$40
	TSB !HitmanDefault

	LDA #$08
	STA !HitmanNormalPlayerIsAbove,x

    SEC
RTL
