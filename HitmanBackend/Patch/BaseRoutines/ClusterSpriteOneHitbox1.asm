ClusterSpriteOneHitbox1:

    STZ !HitmanHB1Right+1
    LDA !HitmanClusterWidthLow,x
    STA !HitmanHB1Right

    STZ !HitmanHB1Bottom+1
    LDA !HitmanClusterHeightLow,x
    STA !HitmanHB1Bottom

    STZ !HitmanHB1Left+1
    LDA !HitmanClusterXOffLow,x
    STA !HitmanHB1Left
    BPL +
    DEC !HitmanHB1Left+1
+

    STZ !HitmanHB1Top+1
    LDA !HitmanClusterYOffLow,x
    STA !HitmanHB1Top
    BPL +
    DEC !HitmanHB1Top+1
+

    LDA !ClusterXHigh,x
    XBA
    LDA !ClusterXLow,x
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

    LDA !ClusterYHigh,x
    XBA
    LDA !ClusterYLow,x
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