SolidForPlayerWithSkip:
    LDA !HitmanNormalPlayerIsAbove,x
    BEQ +
    JSL CheckBounceSurface
RTL
+
    REP #$20
    LDA !HitmanHB2Bottom
    SEC
    SBC !HitmanHB1Top
    CMP #$0009
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
    CMP #$0009
    BCS .CheckHorizontal

.Up
    PHA
    CLC
    ADC !HitmanHB2Bottom
    STA !HitmanHB2Bottom

    LDA !PlayerBlockedStatus_S00MUDLR
    AND #$0004
    BEQ +
    PLA
    SEP #$20
    
    LDA #$08
    TSB !PlayerBlockedStatus_S00MUDLR
    LDA #$80
    TSB !HitmanDefault
    REP #$20
    BRA .CheckHorizontal
+
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

.CheckHorizontal

    LDA !HitmanHB2Right
    SEC
    SBC !HitmanHB1Left
    CMP #$0009
    BCS .CheckRight

.Left
    EOR #$FFFF
    INC A
    PHA
    CLC
    ADC !HitmanHB2Right
    STA !HitmanHB2Right

    LDA !PlayerBlockedStatus_S00MUDLR
    AND #$0002
    BEQ +
    PLA
    SEP #$20
    
    LDA #$01
    TSB !PlayerBlockedStatus_S00MUDLR
    LDA #$20
    TSB !HitmanDefault

    JSL CheckBounceSurface
RTL
+

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
    CMP #$0009
    BCS .end

.Right
    PHA
    CLC
    ADC !HitmanHB2Right
    STA !HitmanHB2Right

    LDA !PlayerBlockedStatus_S00MUDLR
    AND #$0001
    BEQ +
    PLA
    SEP #$20
    
    LDA #$02
    TSB !PlayerBlockedStatus_S00MUDLR
    LDA #$10
    TSB !HitmanDefault

    JSL CheckBounceSurface
RTL
+

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
