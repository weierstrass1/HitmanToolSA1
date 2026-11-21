ClusterSpriteLongOneHitbox2:

    LDA !HitmanClusterWidthLow,x
    STA !HitmanHB2Right
    LDA !HitmanClusterWidthHigh,x
    STA !HitmanHB2Right+1

    LDA !HitmanClusterHeightLow,x
    STA !HitmanHB2Bottom
    LDA !HitmanClusterHeightHigh,x
    STA !HitmanHB2Bottom+1

    LDA !HitmanClusterXOffLow,x
    STA !HitmanHB2Left
    LDA !HitmanClusterXOffHigh,x
    STA !HitmanHB2Left+1

    LDA !HitmanClusterYOffLow,x
    STA !HitmanHB2Top
    LDA !HitmanClusterYOffHigh,x
    STA !HitmanHB2Top+1

    LDA !ClusterXHigh,x
    XBA
    LDA !ClusterXLow,x
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

    LDA !ClusterYHigh,x
    XBA
    LDA !ClusterYLow,x
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