; Rewrite of the complete shared normal-sprite loop used by quake and cape.
; Entry M=X=1; $0E=0 quake, nonzero cape. Original caller uses JSR.
org $0293AE|!rom
    autoclean JML HitmanQuakeInteraction
    NOP

freecode
HitmanQuakeInteraction:
    JSL HitmanQuakeLoopLong
    JML $029403|!rom

HitmanQuakeLoopLong:
    PHB
    PHK
    PLB
    LDX.b #!MaxSprites-1
.Loop
    STX !SpriteIndex
    LDA !SpriteStatus,x
    CMP #$0B
    BEQ .Next
    CMP #$08
    BCC .Next
    LDA !SpriteTweaker166E_LWCFPPPG,x
    AND #$20
    ORA !SpriteYoshiTongueFlag,x
    ORA !SpriteDecTimer2,x
    ORA !SpriteDecTimer7,x
    BNE .Next
    LDA !SpriteBehindEscenaryFlag,x
    LDY !PlayerClimbingFlag_N00SIFHB
    BEQ .NotClimbing
    EOR #$01
.NotClimbing
    EOR !HitmanBehindScenery
    BNE .Next
    LDA $0E
    BNE .CapeBox
    LDY !HitmanQuakeIndex
    JSR HitmanBuildQuakeBox
    BRA .Test
.CapeBox
    JSL Hitman_Base_CapeClippingHB1
.Test
    JSL Hitman_Base_AnyNormalSpriteInteractionOneHitbox
    BCC .Next
    LDA $0E
    BNE .Respond
    LDA !Hitman_Prop_VanillaResponseMask,x
    AND #!HitmanEventQuake
    BEQ .Event
    JSR HitmanQuakeDefaultResponse
.Event
    %HitmanMarkAction(!HitmanEventQuake)
    LDY !HitmanQuakeIndex
    LDA #!HitmanEventQuake
    JSL Hitman_Base_DispatchVanillaCallback
    BRA .Next
.Respond
    JSR HitmanQuakeDefaultResponse
.Next
    LDY !HitmanQuakeIndex
    DEX
    BMI .Done
    JMP .Loop
.Done
    LDX !HitmanQuakeIndex
    PLB
    RTL

; Original quake types 1/2 use 24x24 and 80x16 boxes respectively.
; X=normal receiver, Y=quake slot. HB1 output, X/Y and $0E preserved.
HitmanBuildQuakeBox:
    PHX
    LDA !HitmanQuakeStatus,y
    ASL A
    TAX
    LDA !HitmanQuakeXHigh,y
    XBA
    LDA !HitmanQuakeXLow,y
    REP #$21
    ADC.l .XOffset,x
    CLC
    ADC #$0100
    STA !HitmanHB1Left
    CLC
    ADC.l .Width,x
    STA !HitmanHB1Right
    SEP #$20
    LDA !HitmanQuakeYHigh,y
    XBA
    LDA !HitmanQuakeYLow,y
    REP #$21
    ADC.l .YOffset,x
    CLC
    ADC #$0100
    STA !HitmanHB1Top
    CLC
    ADC.l .Height,x
    STA !HitmanHB1Bottom
    SEP #$20
    PLX
    RTS
.XOffset
    dw $0000,$FFFC,$FFE0
.YOffset
    dw $0000,$FFFC,$FFF8
.Width
    dw $0000,$0018,$0050
.Height
    dw $0000,$0018,$0010

incsrc "QuakeDefaultResponse.asm"
