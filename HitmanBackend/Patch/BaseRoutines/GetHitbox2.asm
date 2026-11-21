GetHitbox2:
    PHB
    ASL
    PHA
    PHY
    SEP #$30

    LDA.b #ContextPosesHitboxes>>16
    PHA
    PLB

    LDA !HitmanSprite2Flip
    ASL
    TAY

    REP #$31

    LDA.w HitBoxXOffsetTables,y
    STA !HitmanHitboxXOffset
    LDA.w HitBoxYOffsetTables,y
    STA !HitmanHitboxYOffset

    PLA
    ASL
    STA $06
    PLA
    TAY

    LDA.w ContextPosesHitboxes,y
    TAY

    LDA.w PoseHitboxesIndexers,y
    CLC
    ADC $06
    TAY

    LDA.w PoseHitboxes,y
    CMP #$FFFF
    BNE .ValidHitbox
    SEP #$30
    PLB
    CLC
RTL
.ValidHitbox
    TAY

    LDA (!HitmanHitboxXOffset),y
    CLC
    ADC !HitmanSprite2X
    STA !HitmanHB2Left
    BPL +
    STZ !HitmanHB2Left
+
    CLC
    ADC.w HitboxWidths,y
    STA !HitmanHB2Right
    BPL +
    LDA #$FF00
    STA !HitmanHB2Left
    STA !HitmanHB2Right
+

    LDA (!HitmanHitboxYOffset),y
    CLC
    ADC !HitmanSprite2Y
    STA !HitmanHB2Top
    BPL +
    STZ !HitmanHB2Top
+
    CLC
    ADC.w HitboxHeights,y
    STA !HitmanHB2Bottom
    BPL +
    LDA #$FF00
    STA !HitmanHB2Top
    STA !HitmanHB2Bottom
+
    SEP #$30
    PLB
    SEC
RTL
