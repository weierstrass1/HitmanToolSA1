PlayerLeftWall:

    REP #$20
    LDA !HitmanHB1Right
    SEC
    SBC !HitmanHB2Left
    BCS +
    SEP #$20
    CLC
RTL
+
    CMP #$0009
    BCC +
    SEP #$20
    CLC
RTL
+
    PHA
    SEP #$20
    LDA !PlayerXSpeed
    BMI +
    PLA
    PLA
    LDA #$02
    TSB !HitmanDefault
    CLC
RTL
+
    REP #$20
    LDA $01,s
    CLC
    ADC !PlayerX
    STA !PlayerX

    LDA $01,s
    CLC
    ADC !HitmanHB2Left
    STA !HitmanHB2Left
    PLA
    CLC
    ADC !HitmanHB2Right
    STA !HitmanHB2Right

    SEP #$20

    LDA #$02
    TSB !PlayerBlockedStatus_S00MUDLR
    STZ !PlayerXSpeed
    LDA #$22
    TSB !HitmanDefault
    SEC
RTL
