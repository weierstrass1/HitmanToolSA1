PlayerRightWall:

    REP #$20
    LDA !HitmanHB1Left
    SEC
    SBC !HitmanHB2Right
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
    SEP #$20
    LDA !PlayerXSpeed
    BEQ ++
    BPL +
++
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

    LDA #$01
    TSB !PlayerBlockedStatus_S00MUDLR
    STZ !PlayerXSpeed
    LDA #$12
    TSB !HitmanDefault
    SEC
RTL
