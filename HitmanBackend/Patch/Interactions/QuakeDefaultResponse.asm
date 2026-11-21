; Vanilla quake/cape response, rewritten with shared DX/Dyzen symbols.
; M=X=1; X=receiver, $0E=source mode. Preserves DBR/D/stack.
HitmanQuakeDefaultResponseLong:
    JSR HitmanQuakeDefaultResponse
    RTL

HitmanQuakeDefaultResponse:
    PHB
    PHK
    PLB
    LDA #$08
    STA !SpriteDecTimer2,X
    LDA !SpriteNumber,X
    CMP #$81
    BNE .OtherSprite
    LDA !SpriteMiscTable3,X
    BEQ .SpecialReturn
    STZ !SpriteMiscTable3,X
    LDA #$C0
    STA !SpriteYSpeed,X
    LDA #$10
    STA !SpriteDecTimer1,X
    STZ !SpriteDirection,X
    LDA #$20
    STA !SpriteDecTimer3,X
.SpecialReturn
    PLB
    RTS
.OtherSprite
    CMP #$2D
    BEQ .Kill
    LDA !SpriteTweaker167A_DPMKSPIS,X
    AND #$02
    BNE .Launch
    LDA !SpriteStatus,X
    CMP #$08
    BEQ .SetStunTimer
    LDA !SpriteNumber,X
    CMP #$0D
    BEQ .Kill
    LDA !SpriteMiscTable3,X
    BEQ .Kill
.SetStunTimer
    LDA #$FF
    STA !SpriteDecTimer1,X
.Kill
    STZ !SpriteDecTimer3,X
    LDA $0E
    CMP #$35
    BEQ .GivePoints
    JSL $01AB6F|!rom
.GivePoints
    LDA #$00
    JSL $02ACE5|!rom
    LDA #$02
    STA !SpriteStatus,X
    LDA !SpriteNumber,X
    CMP #$1E
    BNE .CheckCarryable
    LDA #$1F
    STA !HitmanSpecialKillFlag
.CheckCarryable
    LDA !SpriteTweaker1662_DSCCCCCC,X
    AND #$80
    BNE .Launch
    LDA !SpriteTweaker1656_SSJJCCCC,X
    AND #$10
    BEQ .Launch
    LDA !SpriteTweaker1656_SSJJCCCC,X
    AND #$20
    BNE .Launch
    LDA #$09
    STA !SpriteStatus,X
    ASL !SpriteProperties_YXPPCCCT,X
    SEC
    ROR !SpriteProperties_YXPPCCCT,X
    LDA !SpriteTweaker1686_DNCTSWYE,X
    AND #$40
    BEQ .Launch
    PHX
    LDA !SpriteNumber,X
    TAX
    LDA.l !HitmanVanillaSpriteToSpawn,x
    PLX
    STA !SpriteNumber,X
    JSL $07F78B|!rom
    JSL HitmanRefreshCustomProperties
.Launch
    LDA #$C0
    LDY $0E
    BEQ .SetYSpeed
    LDA #$B0
    CPY #$02
    BNE .SetYSpeed
    LDA #$C0
.SetYSpeed
    STA !SpriteYSpeed,X
    JSL Hitman_Base_SubHorzPos
    LDA .XSpeed,Y
    STA !SpriteXSpeed,X
    TYA
    EOR #$01
    STA !SpriteDirection,X
.Return
    PLB
    RTS
.XSpeed
    db $F8,$08
