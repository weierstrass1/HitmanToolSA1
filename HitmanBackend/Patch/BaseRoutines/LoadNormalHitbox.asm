; Entry M=X=1: X=normal slot, Y=hitbox ordinal.
; Exit M=X=1, preserves X/Y, DBR/D; C=1 HB1 valid, C=0 end/disabled.
; Clobbers A, flags, $04-$07, HB1, sprite2 position, $98-$9B.
; No per-hitbox callbacks. A single-box system has only ordinal zero.
LoadNormalHitbox:
    PHB
    PHY
    PHX
    LDA !Hitman_Prop_HitboxSystem,x
    CMP #$03
    BEQ .Multiple
    CPY #$00
    BNE .End
    CMP #$01
    BEQ .Vanilla
    CMP #$02
    BEQ .ByteBox
    CMP #$04
    BEQ .WordBox
.End
    SEP #$30
    PLX
    PLY
    PLB
    CLC
    RTL
.Vanilla
    SEP #$30
    JSL $03B6E5|!rom
    LDA $00
    STA !HitmanHB1Left
    LDA $08
    STA !HitmanHB1Left+1
    LDA $01
    STA !HitmanHB1Top
    LDA $09
    STA !HitmanHB1Top+1
    LDA $03
    STA $06
    STZ $07
    STZ $03
    REP #$21
    LDA !HitmanHB1Left
    ADC #$0100
    STA !HitmanHB1Left
    CLC
    ADC $02
    STA !HitmanHB1Right
    LDA !HitmanHB1Top
    CLC
    ADC #$0100
    STA !HitmanHB1Top
    LDA !HitmanHB1Top
    CLC
    ADC $06
    STA !HitmanHB1Bottom
    BRA .Valid
.ByteBox
    SEP #$30
    JSL NormalSpriteOneHitbox1
    BRA .Valid
.WordBox
    SEP #$30
    JSL NormalSpriteLongOneHitbox1
.Valid
    SEP #$30
    PLX
    PLY
    PLB
    SEC
    RTL
.Multiple
    JSL NormalSpritePositionSetup2
    LDA !HitmanNormalContextIDHigh,x
    XBA
    LDA !HitmanNormalContextIDLow,x
    REP #$30
    ASL A
    TAY
    SEP #$20
    LDA.b #bank(ContextPosesHitboxes)
    PHA
    PLB
    REP #$20
    LDA.w ContextPosesHitboxes,y
    TAY
    LDA $02,s
    AND #$00FF
    ASL A
    CLC
    ADC.w PoseHitboxesIndexers,y
    TAY
    LDA.w PoseHitboxes,y
    CMP #$FFFF
    BNE .ValidOrdinal
    JMP .End
.ValidOrdinal
    PHA
    LDA !HitmanNormalFlip,x
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
    ADC !HitmanSprite2X
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
    ADC !HitmanSprite2Y
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
    JMP .Valid
