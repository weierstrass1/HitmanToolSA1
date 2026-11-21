; Full rewrite of ProcessFireball, entered by the original bank-02 JSR.
; M=X=1, X=extended slot. Preserves DBR/D/stack; restores X and SpriteIndex.
org $02A0AC|!rom
    autoclean JML HitmanFireballInteraction
    NOP

freecode
HitmanFireballInteraction:
    JSL HitmanFireballLong
    JML $02A150|!rom

HitmanFireballLong:
    TXA
    EOR !TrueFrameCounter
    AND #$03
    BEQ .Start
    RTL
.Start
    PHB
    PHK
    PLB
    PHX
    LDA !ExtendedNumber,x
    PHA
    TAX
    LDA.l ExtendedFireballEvent,x
    BNE .HasEvent
    PLA
    PLX
    PLB
    RTL
.HasEvent
    PHA
    LDA $03,s
    STA !HitmanFireballIndex
    LDA !SpriteIndex
    PHA
    LDX.b #!MaxSprites-3
.Loop
    STX !SpriteIndex
    LDA !SpriteStatus,x
    CMP #$08
    BCS .Active
    JMP .Next
.Active
    LDA !SpriteTweaker167A_DPMKSPIS,x
    AND #$02
    ORA !SpriteYoshiTongueFlag,x
    BEQ .Available
    JMP .Next
.Available
    LDY !HitmanFireballIndex
    LDA !SpriteBehindEscenaryFlag,x
    EOR !ExtendedBehindLayersFlag,y
    BEQ .SamePlane
    JMP .Next
.SamePlane
    JSL Hitman_Base_CheckExtendedNormalPair
    BCS .Contact
    JMP .Next
.Contact
    LDY !HitmanFireballIndex
    LDA $02,s
    PHA
    AND !Hitman_Prop_VanillaResponseMask,x
    BEQ .AfterResponse
    LDA $01,s
    JSR .DefaultResponse
.AfterResponse
    PLA
    PHA
    AND !Hitman_Prop_VanillaEventMask,x
    ORA !Hitman_Prop_ActionFlag,x
    STA !Hitman_Prop_ActionFlag,x
    PLA
    LDY !HitmanFireballIndex
    JSL Hitman_Base_DispatchVanillaCallback
    LDY !HitmanFireballIndex
    LDA !ExtendedNumber,y
    CMP $03,s
    BEQ .Next
    BRA .Done
.DefaultResponse
    CMP #!HitmanEventYoshiFireball
    BEQ .Damage
    ; Vanilla consumption turns Mario's projectile into its smoke state.
    LDA #$01
    STA !HitmanSound1
    STA !ExtendedNumber,y
    LDA #$0F
    STA !ExtendedMiscTable2,y
.Damage
    LDA !SpriteTweaker166E_LWCFPPPG,x
    AND #$10
    BNE .ReturnResponse
    LDA !SpriteTweaker190F_WCDJ5SDP,x
    AND #$08
    BEQ .Coin
    INC !SpriteMiscTable7,x
    LDA !SpriteMiscTable7,x
    CMP #$05
    BCC .ReturnResponse
    LDA #$02
    STA !HitmanSound1
    STA !SpriteStatus,x
    LDA #$D0
    STA !SpriteYSpeed,x
    JSL Hitman_Base_SubHorzPos
    LDA.w .DeathSpeed,y
    STA !SpriteXSpeed,x
    LDA #$04
    JSL $02ACE5|!rom
    RTS
.Coin
    ; Switch PIXI slots to vanilla before initializing the coin.
    LDA !ExtraBits,x
    AND #$F7
    STA !ExtraBits,x
    LDA #$03
    STA !HitmanSound1
    LDA #$21
    STA !SpriteNumber,x
    LDA #$08
    STA !SpriteStatus,x
    JSL $07F7D2|!rom
    LDA #$D0
    STA !SpriteYSpeed,x
    JSL Hitman_Base_SubHorzPos
    TYA
    EOR #$01
    STA !SpriteDirection,x
.ReturnResponse
    RTS
.Next
    LDY !HitmanFireballIndex
    DEX
    BMI .Done
    JMP .Loop
.Done
    PLA
    STA !SpriteIndex
    PLA
    PLA
    PLX
    PLB
    RTL
.DeathSpeed
    db $F0,$10
