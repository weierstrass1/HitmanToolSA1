; Rewrite of sprite-pair traversal. Both slots use their configured geometry.
; M=X=1, DBR/D/stack preserved; return through original bank-01 RTS.
org $01A411|!rom
    autoclean JML HitmanSpriteSpriteInteraction
    NOP
freecode
HitmanSpriteSpriteInteraction:
    TXA
    EOR !TrueFrameCounter
    LSR A
    BCS .Start
    JML $01A40A|!rom
.Start
    PHB
    PHK
    PLB
    LDA !SpriteStatus,x
    CMP #$08
    BCS .CheckStatus
    JMP .Done
.CheckStatus
    CMP #$0C
    BCC .Active
    JMP .Done
.Active
    DEX
.Loop
    LDY !SpriteIndex
    LDA !SpriteStatus,x
    CMP #$08
    BCC .SkipInactive
    CMP #$0D
    BCC .CheckPairFlags
.SkipInactive
    JMP .Next
.CheckPairFlags
    LDA !SpriteTweaker1686_DNCTSWYE,x
    ORA !SpriteTweaker1686_DNCTSWYE,y
    AND #$08
    ORA !SpriteDecTimer4,x
    ORA !SpriteDecTimer4,y
    ORA !SpriteYoshiTongueFlag,x
    BNE .Next
    LDA !SpriteBehindEscenaryFlag,x
    EOR !SpriteBehindEscenaryFlag,y
    BNE .Next
    STX !HitmanOtherSpriteIndex
    LDA.w HitmanPairRowBase,y
    CLC
    ADC.w HitmanPairSlotByte,x
    TXY
    TAX
    LDA.l !HitmanPairMask,x
    AND.w HitmanPairBit,y
    BNE .PairAllowed
    LDX !HitmanOtherSpriteIndex
    JMP .Next
.PairAllowed
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    JSL Hitman_Base_CheckNormalPair
    BCC .Next
    JSR CalculatePairFacing
    PHA
    LDA #$00
    PHA
    PHA
    JSR .Collission
    PLA
    BEQ .CallbackCurrent
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    JSL Hitman_Base_DispatchVanillaCallback
.CallbackCurrent
    PLA
    BEQ .CallbacksDone
    LDX !SpriteIndex
    LDY !HitmanOtherSpriteIndex
    JSL Hitman_Base_DispatchVanillaCallback
.CallbacksDone
    PLA
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    JSR NotifySurvivingItemContact
    LDX !SpriteIndex
    LDY !HitmanOtherSpriteIndex
    JSR NotifySurvivingItemContact
    LDX !HitmanOtherSpriteIndex
.Next
    DEX
    BMI .Done
    JMP .Loop
.Done
    LDA #$FF
    STA !HitmanOtherSpriteIndex
    LDX !SpriteIndex
    PLB
    JML $01A40A|!rom
.Collission
    LDA !SpriteStatus,y
    SEC
    SBC #$08
    STA $00
    ASL
    ASL
    CLC
    ADC $00
    STA $00

    LDA !SpriteStatus,x
    SEC
    SBC #$08
    CLC
    ADC $00
    ASL
    TAX

    JMP (..CollissionRoutine,x)

..Return
    LDX !HitmanOtherSpriteIndex
RTS

..CollissionRoutine
    dw ..BothNormal
    dw ..CurrentNormalOtherStationary
    dw ..CurrentNormalOtherKicked
    dw ..CurrentNormalOtherCarried
    dw ..Return
    dw ..CurrentStationaryOtherNormal
    dw ..BothStationary
    dw ..CurrentStationaryOtherKicked
    dw ..CurrentNormalOtherCarried
    dw ..Return
    dw ..CurrentKickedOtherNormal
    dw ..CurrentKickedOtherStationary
    dw ..CurrentNormalOtherCarried
    dw ..CurrentNormalOtherCarried
    dw ..Return
    dw ..CurrentNormalOtherCarried
    dw ..CurrentNormalOtherCarried
    dw ..CurrentNormalOtherCarried
    dw ..CurrentNormalOtherCarried
    dw ..Return

..CurrentStationaryOtherNormal
    LDA !SpriteBlockedStatus_ASB0UDLR,y
    AND #$04
    BNE ..CurrentNormalOtherStationary
    LDA !SpriteNumber|!dp,y
    CMP #$0F
    BEQ ..GoToCurrentNormalOtherCarried
    JMP ..CurrentKickedOtherNormal
..BothStationary
    LDA !SpriteNumber|!dp,y
    CMP #$0F
    BEQ ..GoToCurrentNormalOtherCarried
    LDA !SpriteBlockedStatus_ASB0UDLR,y
    AND #$04
    BNE ..GoToCurrentNormalOtherKicked
    JMP ..CurrentKickedOtherStationary
..GoToCurrentNormalOtherKicked
    JMP ..CurrentNormalOtherKicked

..CurrentStationaryOtherKicked
    LDA !SpriteBlockedStatus_ASB0UDLR,y
    AND #$04
    BEQ ..GoToCurrentNormalOtherCarried
    JMP ..CurrentNormalOtherKicked
..GoToCurrentNormalOtherCarried
    JMP ..CurrentNormalOtherCarried

..CurrentNormalOtherStationary
    LDX !HitmanOtherSpriteIndex
    JSR KickOrHopIntoShell

    LDY !HitmanOtherSpriteIndex
    LDX !SpriteIndex
    JSR KickOrHopIntoShell

    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex

    LDA !SpriteDecTimer3,x
    ORA !SpriteDecTimer3,y
    BEQ +
RTS
+
    LDA !SpriteStatus,x
    CMP #$09
    BNE ..BothNormal
    LDA !SpriteBlockedStatus_ASB0UDLR,x
    AND #$04
    BNE ..BothNormal
    LDA !SpriteNumber,x
    CMP #$0F
    BEQ ..GoToCurrentNormalOtherCarried
    JMP ..CurrentNormalOtherKicked

..BothNormal
    LDA #!HitmanEventRegular
    STA $03,s
    STA $04,s
    LDA $05,s
    PHA
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    %HitmanMarkAction(!HitmanEventRegular)
    LDA !Hitman_Prop_VanillaResponseMask,x
    AND #!HitmanEventRegular
    BEQ ...CurrentReceiver
    LDA $01,s
    EOR #$01
    JSR ApplyRegularFlip
...CurrentReceiver
    LDX !SpriteIndex
    LDY !HitmanOtherSpriteIndex
    %HitmanMarkAction(!HitmanEventRegular)
    LDA !Hitman_Prop_VanillaResponseMask,x
    AND #!HitmanEventRegular
    BEQ ...Done
    LDA $01,s
    JSR ApplyRegularFlip
...Done
    PLA
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    RTS

..CurrentNormalOtherKicked
    LDA #!HitmanEventShell
    STA $04,s
    JSR MarkCurrentShell
    BCC ...Default
    RTS
...Default
    LDX !HitmanOtherSpriteIndex
    LDA !SpriteNumber|!dp,y
    SEC
    SBC #$83
    CMP #$02
    BCS +
    JSR FlipSpriteDir
    STZ !SpriteYSpeed,x
    PHX
    TYX
    JSR QuestionBlockCollission
    PLX
RTS
+
    LDX !SpriteIndex
    LDY !HitmanOtherSpriteIndex
    JSR BlueShellLessKoopaKick

    LDA #$02
    STA !SpriteStatus,y
    PHX
    TYX
    JSL $01AB72|!rom
    PLX
    LDA !SpriteXSpeed,x
    ASL
    LDA #$10
    BCC +
    LDA #$F0
+
    STA !SpriteXSpeed|!dp,y
    LDA #$D0
    STA !SpriteYSpeed|!dp,y
    PHY
    INC !SpriteMiscTable13,x
    LDY !SpriteMiscTable13,x
    CPY #$08
    BCS +
    LDA ...SoundEffects,y
    STA !HitmanSound1
+
    TYA
    CMP #$08
    BCC +
    LDA #$08
+
    PLY
    JSL $02ACE1|!rom
RTS

...SoundEffects
    db $60,$13,$14,$15,$16,$17,$18,$19

..CurrentKickedOtherNormal
    LDA #!HitmanEventShell
    STA $03,s
    JSR MarkOtherShell
    BCC ...Default
    RTS
...Default
    LDX !HitmanOtherSpriteIndex
    LDA !SpriteNumber,x
    SEC
    SBC #$83
    CMP #$02
    BCS +
    PHX
    TYX
    JSR FlipSpriteDir
    PLX
    LDA #$00
    STA !SpriteYSpeed|!dp,y
    JSR QuestionBlockCollission
RTS

+
    JSR BlueShellLessKoopaKick
    JMP ..CurrentKickedOtherStationary_KillOther

..CurrentKickedOtherStationary
    LDX !HitmanOtherSpriteIndex
    LDA !SpriteBlockedStatus_ASB0UDLR,x
    AND #$04
    BNE +
    JMP ..CurrentNormalOtherCarried
+
    LDA #!HitmanEventShell
    STA $03,s
    JSR MarkOtherShell
    BCC ...KillOther
    RTS
...KillOther
    LDX !HitmanOtherSpriteIndex
    PHX
    LDA !SpriteMiscTable13,y
    INC A
    STA !SpriteMiscTable13,y
    LDX !SpriteMiscTable13,y
    CPX #$08
    BCS +
    LDA ..CurrentNormalOtherKicked_SoundEffects,x
    STA !HitmanSound1
+
    TXA
    CMP #$08
    BCC +
    LDA #$08
+
    PLX
    JSL $02ACE5|!rom
    LDA #$02
    STA !SpriteStatus,x
    JSL $01AB72|!rom
    LDA !SpriteXSpeed|!dp,y
    ASL
    LDA #$10
    BCC +
    LDA #$F0
+
    STA !SpriteXSpeed,x
    LDA #$D0
    STA !SpriteYSpeed,x
RTS

..CurrentNormalOtherCarried
    ; Keep knockback and per-pair default activity on the stack across callbacks.
    LDX !HitmanOtherSpriteIndex
    LDA !SpriteXSpeed,x
    ASL A
    LDA #$10
    BCS ...HaveSpeed
    LDA #$F0
...HaveSpeed
    PHA
    LDA #$00
    PHA
    LDA #!HitmanEventShell
    STA $05,s
    JSR MarkOtherShell
    BCS ...Current
    LDA #$01
    STA $01,s
    LDA !SpriteNumber,x
    CMP #$83
    BEQ ...OtherQuestion
    CMP #$84
    BNE ...KillOther
...OtherQuestion
    JSR QuestionBlockCollission
    BRA ...OtherSpeed
...KillOther
    LDA #$02
    STA !SpriteStatus,x
    LDA #$D0
    STA !SpriteYSpeed,x
...OtherSpeed
    LDA $02,s
    STA !SpriteXSpeed,x
...Current
    LDA #!HitmanEventShell
    STA $06,s
    JSR MarkCurrentShell
    BCS ...Effects
    LDA #$01
    STA $01,s
    LDX !SpriteIndex
    LDA !SpriteNumber,x
    CMP #$80
    BEQ ...CurrentSpeed
    CMP #$83
    BEQ ...CurrentQuestion
    CMP #$84
    BNE ...KillCurrent
...CurrentQuestion
    JSR QuestionBlockCollission
    BRA ...CurrentSpeed
...KillCurrent
    LDA #$02
    STA !SpriteStatus,x
    LDA #$D0
    STA !SpriteYSpeed,x
...CurrentSpeed
    LDA $02,s
    EOR #$FF
    INC A
    STA !SpriteXSpeed,x
...Effects
    LDA $01,s
    BEQ ...Done
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    JSL $01AB6F|!rom
    LDA #$04
    JSL $02ACE5|!rom
...Done
    PLA
    PLA
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    RTS

KickOrHopIntoShell:
    STY $00
    LDA !SpriteBlockedStatus_ASB0UDLR,x
    AND #$04
    BEQ .Return
    LDA !SpriteBlockedStatus_ASB0UDLR,y
    AND #$04
    BEQ .Return
    LDA !SpriteTweaker1656_SSJJCCCC,x
    AND #$40
    BEQ .Return
    LDA !SpriteDecTimer3,y
    ORA !SpriteDecTimer3,x
    BNE .Return
    STZ $02
    LDA !SpriteXLow,x
    SEC
    SBC !SpriteXLow|!dp,y
    BMI +
    INC $02
+
    CLC
    ADC #$08
    CMP #$10
    BCC .Return
    LDA !SpriteDirection,x
    CMP $02
    BNE .Return
    LDA !SpriteNumber,x
    CMP #$02
    BNE .HopIntoShell
.Kicking
    LDA #$20
    STA !SpriteDecTimer6,x
    STA !SpriteDecTimer3,x
    LDA #$23
    STA !SpriteDecTimer4,x
    TYA
    STA !SpriteMiscTable12,x
.Return
RTS

.HopIntoShell
    LDA !SpriteDecTimer1,y
    BNE ..Return
    LDA !SpriteNumber|!dp,y
    CMP #$0F
    BCS ..Return
    LDA !SpriteProperties_YXPPCCCT,y
    BPL +
    AND #$7F
    STA !SpriteProperties_YXPPCCCT,y
    LDA #$E0
    STA !SpriteYSpeed|!dp,y
    LDA #$20
    STA !SpriteDecTimer4,y
    LDA #$20
    STA !SpriteMiscTable3,x
    STA !SpriteDecTimer3,x
RTS

+
    LDA #$E0
    STA !SpriteYSpeed,x
    LDA !SpriteInLiquidFlag,x
    CMP #$01
    LDA #$18
    BCC +
    LDA #$2C
+
    STA !SpriteDecTimer3,x
    TXA
    STA !SpriteMiscTable10,y
    TYA
    STA !SpriteMiscTable10,x
..Return
RTS

BlueShellLessKoopaKick:
    LDA !SpriteNumber,x
    CMP #$02
    BNE .RestoreIndexes
    LDA !SpriteMiscTable14,y
    BNE .RestoreIndexes
    LDA !SpriteDirection,x
    CMP !SpriteDirection,y
    BEQ .RestoreIndexes
    STY $01
    LDY !SpriteMiscTable8,x
    BNE .ReturnFromMainRoutine

    STZ !SpriteMiscTable7,x
    STZ !SpriteDecTimer6,x

    TAY
    STY $00
    LDA !SpriteXLow,x
    CLC
    ADC .XOffsetLowByte,y
    LDY $01
    STA !SpriteXLow|!dp,y
    LDA !SpriteXHigh,x
    LDY $00
    ADC .XOffsetHighByte,y
    LDY $01
    STA !SpriteXHigh,y
    TYA
    STA !SpriteMiscTable12,x
    LDA #$01
    STA !SpriteMiscTable8,x
.ReturnFromMainRoutine
    PLA
    PLA
.RestoreIndexes
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
RTS

.XOffsetLowByte
    db $10,$F0
.XOffsetHighByte
    db $00,$FF

FlipSpriteDir:
    LDA !SpriteDecTimer5,x
    BNE +
    LDA #$08
    STA !SpriteDecTimer5,x
    LDA !SpriteXSpeed,x
    EOR #$FF
    INC A
    STA !SpriteXSpeed,x
    LDA !SpriteDirection,x
    EOR #$01
    STA !SpriteDirection,x
+
RTS

QuestionBlockCollission:
    LDA #$0F
    STA !SpriteDecTimer4,X
    LDA !SpriteMiscTable3,X
    BNE +
    INC !SpriteMiscTable3,X
    LDA #$10
    STA !SpriteDecTimer3,X
+
    LDA #$01
    STA !HitmanSound1
    CLC
RTS

; Event helpers restore the original X=other, Y=current convention.
MarkCurrentShell:
    LDX !SpriteIndex
    LDY !HitmanOtherSpriteIndex
    %HitmanMarkAction(!HitmanEventShell)
    LDA !Hitman_Prop_VanillaResponseMask,x
    AND #!HitmanEventShell
    CMP #$01
    BCC .Restore
    CLC
    BRA .Return
.Restore
    SEC
.Return
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    RTS
MarkOtherShell:
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    %HitmanMarkAction(!HitmanEventShell)
    LDA !Hitman_Prop_VanillaResponseMask,x
    AND #!HitmanEventShell
    CMP #$01
    BCC .Restore
    CLC
    BRA .Return
.Restore
    SEC
.Return
    LDX !HitmanOtherSpriteIndex
    LDY !SpriteIndex
    RTS
; X=participant, Y=the other sprite. Only notify surviving stunned/kicked sprites.
NotifySurvivingItemContact:
    LDA !SpriteStatus,x
    SEC
    SBC #$09
    CMP #$02
    BCS .Return
    LDA !Hitman_Prop_VanillaEventMask,x
    AND #!HitmanEventShellHit
    BEQ .Return
    ORA !Hitman_Prop_ActionFlag,x
    STA !Hitman_Prop_ActionFlag,x
    LDA #!HitmanEventShellHit
    JSL Hitman_Base_DispatchVanillaCallback
.Return
    RTS

ApplyRegularFlip:
    PHA
    LDA !SpriteTweaker1686_DNCTSWYE,x
    AND #$10
    ORA !SpriteDecTimer5,x
    BNE .Skip
    LDA !SpriteXSpeed,x
    BEQ .Skip
    LDA !SpriteDirection,x
    AND #$02
    ORA $01,s
    CMP !SpriteDirection,x
    BEQ .Skip
    STA !SpriteDirection,x
    LDA #$08
    STA !SpriteDecTimer5,x
    PLA
    RTS
.Skip
    PLA
    RTS

CalculatePairFacing:
    ; Use actual contact rectangles before the state dispatch uses scratch.
    REP #$20
    LDA !HitmanHB1Right
    SEC
    SBC !HitmanHB1Left
    LSR A
    CLC
    ADC !HitmanHB1Left
    PHA
    LDA !HitmanHB2Right
    SEC
    SBC !HitmanHB2Left
    LSR A
    CLC
    ADC !HitmanHB2Left
    CMP $01,s
    PLA
    LDA #$0000
    ROL A
    SEP #$20
    RTS

; Byte-aligned rows belong to the higher slot. The lower slot selects a byte and bit.
HitmanPairRowBase:
    db $00,$00,$01,$02,$03,$04,$05,$06
    db $07,$08,$0A,$0C,$0E,$10,$12,$14
    db $16,$18,$1B,$1E,$21,$24
HitmanPairSlotByte:
    db $00,$00,$00,$00,$00,$00,$00,$00
    db $01,$01,$01,$01,$01,$01,$01,$01
    db $02,$02,$02,$02,$02,$02
HitmanPairBit:
    db $01,$02,$04,$08,$10,$20,$40,$80
    db $01,$02,$04,$08,$10,$20,$40,$80
    db $01,$02,$04,$08,$10,$20
