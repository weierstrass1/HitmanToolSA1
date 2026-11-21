SolidForPlayer:
    LDA !HitmanNormalPlayerIsAbove,x
    BEQ +
    JSL CheckBounceSurface
RTL
+
    REP #$20
    LDA !HitmanHB2Bottom
    SEC
    SBC !HitmanHB1Top
    CMP #$0005
    BCS .CheckUp
.Down
    SEP #$20

    LDA #$08
    STA !HitmanNormalPlayerIsAbove,x

    JSL CheckBounceSurface
RTL

.CheckUp

    LDA !HitmanHB1Bottom
    SEC
    SBC !HitmanHB2Top
    CMP #$0005
    BCS .CheckVertical

.Up
    PHA
    CLC
    ADC !HitmanHB2Bottom
    STA !HitmanHB2Bottom

    LDA $01,s
    CLC
    ADC !HitmanHB2Top
    STA !HitmanHB2Top

    PLA
    CLC
    ADC !PlayerY
    STA !PlayerY
    SEP #$20

    LDA #$08
    TSB !PlayerBlockedStatus_S00MUDLR
    LDA #$80
    TSB !HitmanDefault
    REP #$20

.CheckVertical

    LDA !HitmanHB2Right
    SEC
    SBC !HitmanHB1Left
    CMP #$0005
    BCS .CheckRight

.Left
    EOR #$FFFF
    INC A
    PHA
    CLC
    ADC !HitmanHB2Right
    STA !HitmanHB2Right

    LDA $01,s
    CLC
    ADC !HitmanHB2Left
    STA !HitmanHB2Left

    PLA
    CLC
    ADC !PlayerX
    STA !PlayerX
    SEP #$20

    LDA #$01
    TSB !PlayerBlockedStatus_S00MUDLR
    LDA #$20
    TSB !HitmanDefault

    JSL CheckBounceSurface
RTL

.CheckRight

    LDA !HitmanHB1Right
    SEC
    SBC !HitmanHB2Left
    CMP #$0005
    BCS .end

.Right
    PHA
    CLC
    ADC !HitmanHB2Right
    STA !HitmanHB2Right

    LDA $01,s
    CLC
    ADC !HitmanHB2Left
    STA !HitmanHB2Left

    PLA
    CLC
    ADC !PlayerX
    STA !PlayerX
    SEP #$20

    LDA #$02
    TSB !PlayerBlockedStatus_S00MUDLR
    LDA #$10
    TSB !HitmanDefault

    JSL CheckBounceSurface
RTL

.end
    SEP #$20
    JSL CheckBounceSurface
RTL
