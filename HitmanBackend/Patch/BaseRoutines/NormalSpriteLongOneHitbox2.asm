NormalSpriteLongOneHitbox2:

    LDA !HitmanNormalWidthLow,x
    STA !HitmanHB2Right
    LDA !HitmanNormalWidthHigh,x
    STA !HitmanHB2Right+1

    LDA !HitmanNormalHeightLow,x
    STA !HitmanHB2Bottom
    LDA !HitmanNormalHeightHigh,x
    STA !HitmanHB2Bottom+1

    LDA !HitmanNormalXOffLow,x
    STA !HitmanHB2Left
    LDA !HitmanNormalXOffHigh,x
    STA !HitmanHB2Left+1

    LDA !HitmanNormalYOffLow,x
    STA !HitmanHB2Top
    LDA !HitmanNormalYOffHigh,x
    STA !HitmanHB2Top+1

    LDA !SpriteXHigh,x
    XBA
    LDA !SpriteXLow,x
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

    LDA !SpriteYHigh,x
    XBA
    LDA !SpriteYLow,x
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