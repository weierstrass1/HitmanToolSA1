NormalSpriteLongOneHitbox1:

    LDA !HitmanNormalWidthLow,x
    STA !HitmanHB1Right
    LDA !HitmanNormalWidthHigh,x
    STA !HitmanHB1Right+1

    LDA !HitmanNormalHeightLow,x
    STA !HitmanHB1Bottom
    LDA !HitmanNormalHeightHigh,x
    STA !HitmanHB1Bottom+1

    LDA !HitmanNormalXOffLow,x
    STA !HitmanHB1Left
    LDA !HitmanNormalXOffHigh,x
    STA !HitmanHB1Left+1

    LDA !HitmanNormalYOffLow,x
    STA !HitmanHB1Top
    LDA !HitmanNormalYOffHigh,x
    STA !HitmanHB1Top+1

    LDA !SpriteXHigh,x
    XBA
    LDA !SpriteXLow,x
    REP #$21
    ADC !HitmanHB1Left
    CLC
    ADC #$0100
    STA !HitmanHB1Left
    BPL +
    STZ !HitmanHB1Left
+
    CLC
    ADC !HitmanHB1Right
    STA !HitmanHB1Right
    BPL +
    LDA #$FF00
    STA !HitmanHB1Left
    STA !HitmanHB1Right
+
    SEP #$20

    LDA !SpriteYHigh,x
    XBA
    LDA !SpriteYLow,x
    REP #$21
    ADC !HitmanHB1Top
    CLC
    ADC #$0100
    STA !HitmanHB1Top
    BPL +
    STZ !HitmanHB1Top
+
    CLC
    ADC !HitmanHB1Bottom
    STA !HitmanHB1Bottom
    BPL +
    LDA #$FF00
    STA !HitmanHB1Top
    STA !HitmanHB1Bottom
+
    SEP #$20

RTL