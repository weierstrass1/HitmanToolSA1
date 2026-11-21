ExtendedSpriteLongOneHitbox2:

    LDA !HitmanExtendedWidthLow,x
    STA !HitmanHB2Right
    LDA !HitmanExtendedWidthHigh,x
    STA !HitmanHB2Right+1

    LDA !HitmanExtendedHeightLow,x
    STA !HitmanHB2Bottom
    LDA !HitmanExtendedHeightHigh,x
    STA !HitmanHB2Bottom+1

    LDA !HitmanExtendedXOffLow,x
    STA !HitmanHB2Left
    LDA !HitmanExtendedXOffHigh,x
    STA !HitmanHB2Left+1

    LDA !HitmanExtendedYOffLow,x
    STA !HitmanHB2Top
    LDA !HitmanExtendedYOffHigh,x
    STA !HitmanHB2Top+1

    LDA !ExtendedXHigh,x
    XBA
    LDA !ExtendedXLow,x
    REP #$21
    ADC !HitmanHB2Left
    CLC
    ADC #$0100
    STA !HitmanHB2Left
    BPL +
    STZ !HitmanHB2Left
+
    CLC
    ADC !HitmanHB2Right
    STA !HitmanHB2Right
    BPL +
    LDA #$FF00
    STA !HitmanHB2Left
    STA !HitmanHB2Right
+
    SEP #$20

    LDA !ExtendedYHigh,x
    XBA
    LDA !ExtendedYLow,x
    REP #$21
    ADC !HitmanHB2Top
    CLC
    ADC #$0100
    STA !HitmanHB2Top
    BPL +
    STZ !HitmanHB2Top
+
    CLC
    ADC !HitmanHB2Bottom
    STA !HitmanHB2Bottom
    BPL +
    LDA #$FF00
    STA !HitmanHB2Top
    STA !HitmanHB2Bottom
+
    SEP #$20

RTL