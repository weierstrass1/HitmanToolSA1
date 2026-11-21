; M=X=1, X=normal receiver, Y=extended source. C=contact.
; Preserves X/Y, DBR/D and stack. Clobbers A, flags and collision scratch.
; Only checks geometry. Neither actor's action flags or callbacks are touched.
CheckExtendedNormalPair:
    LDA !Hitman_Prop_HitboxSystem,x
    BEQ .Disabled
    PHX
    PHY
    TYX
    LDA !ExtendedNumber,x
    TAX
    LDA.l ExtendedHitboxSystem,x
    BEQ .Miss
    CMP #$01
    BEQ .Table
    TYX
    CMP #$02
    BEQ .ByteBox
    CMP #$03
    BEQ .Multiple
    JSL ExtendedSpriteLongOneHitbox1
    BRA .CheckSingle
.ByteBox
    JSL ExtendedSpriteOneHitbox1
    BRA .CheckSingle
.Table
    TYX
    JSR .LoadTable
.CheckSingle
    LDA $02,s
    TAX
    JSL AnyNormalSpriteInteractionOneHitbox
    BRA .Return
.Multiple
    LDY #$00
.Loop
    LDA $01,s
    TAX
    PHY
    JSR .LoadPose
    BCC .EndPose
    LDA $03,s
    TAX
    JSL AnyNormalSpriteInteractionOneHitbox
    PLY
    BCS .Return
    INY
    BNE .Loop
    BRA .Miss
.EndPose
    PLY
.Miss
    CLC
.Return
    PLY
    PLX
    RTL
.Disabled
    CLC
    RTL

.LoadTable
    JSL ExtendedPositionSetup
    LDA !ExtendedNumber,x
    REP #$30
    AND #$00FF
    ASL A
    TAX
    LDA.l ExtendedXOffset,x
    CLC
    ADC !HitmanSprite1X
    STA !HitmanHB1Left
    LDA.l ExtendedWidth,x
    CLC
    ADC !HitmanHB1Left
    STA !HitmanHB1Right
    LDA.l ExtendedYOffset,x
    CLC
    ADC !HitmanSprite1Y
    STA !HitmanHB1Top
    LDA.l ExtendedHeight,x
    CLC
    ADC !HitmanHB1Top
    STA !HitmanHB1Bottom
    SEP #$30
    RTS

.LoadPose
    PHB
    JSL ExtendedPositionSetup
    LDA !HitmanExtendedContextIDHigh,x
    XBA
    LDA !HitmanExtendedContextIDLow,x
    REP #$30
    PHY
    ASL A
    TAY
    SEP #$20
    LDA.b #bank(ContextPosesHitboxes)
    PHA
    PLB
    REP #$20
    LDA.w ContextPosesHitboxes,y
    TAY
    PLA
    ASL A
    CLC
    ADC.w PoseHitboxesIndexers,y
    TAY
    LDA.w PoseHitboxes,y
    CMP #$FFFF
    BNE .ValidOrdinal
    BRA .PoseEnd
.ValidOrdinal
    PHA
    LDA !HitmanExtendedFlip,x
    AND #$0003
    ASL A
    TAY
    LDA.w HitBoxXOffsetTables,y
    STA !HitmanHitboxXOffset
    LDA.w HitBoxYOffsetTables,y
    STA !HitmanHitboxYOffset
    PLY
    LDA (!HitmanHitboxXOffset),y
    CLC
    ADC !HitmanSprite1X
    STA !HitmanHB1Left
    BPL .LeftValid
    STZ !HitmanHB1Left
.LeftValid
    CLC
    ADC.w HitboxWidths,y
    STA !HitmanHB1Right
    BPL .RightValid
    LDA #$FF00
    STA !HitmanHB1Left
    STA !HitmanHB1Right
.RightValid
    LDA (!HitmanHitboxYOffset),y
    CLC
    ADC !HitmanSprite1Y
    STA !HitmanHB1Top
    BPL .TopValid
    STZ !HitmanHB1Top
.TopValid
    CLC
    ADC.w HitboxHeights,y
    STA !HitmanHB1Bottom
    BPL .BottomValid
    LDA #$FF00
    STA !HitmanHB1Top
    STA !HitmanHB1Bottom
.BottomValid
    SEP #$30
    PLB
    SEC
    RTS
.PoseEnd
    SEP #$30
    PLB
    CLC
    RTS
