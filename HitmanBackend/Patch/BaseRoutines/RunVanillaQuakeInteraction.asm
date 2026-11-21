; M=X=1. X = quake slot (0..3). Preserve the caller's X, SpriteIndex and quake mode.
RunVanillaQuakeInteraction:
    LDA !HitmanQuakeStatus,x
    BEQ .Return
    DEC !HitmanQuakeTimer,x
    BNE .CheckTimer
    STZ !HitmanQuakeStatus,x
.Return
    RTL
.CheckTimer
    LDA !HitmanQuakeTimer,x
    CMP #$03
    BCS .Return

    LDA !SpriteIndex
    PHA
    LDA !HitmanQuakeIndex
    PHA
    LDA $0E
    PHA
    STX !HitmanQuakeIndex
    STZ $0E
    JSL HitmanQuakeLoopLong
    PLA
    STA $0E
    PLA
    STA !HitmanQuakeIndex
    PLA
    STA !SpriteIndex
    RTL
