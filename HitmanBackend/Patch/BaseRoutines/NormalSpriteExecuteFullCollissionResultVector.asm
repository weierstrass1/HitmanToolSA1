;Can execute a collission result from the vector if the current sprite
;and the other sprite that is colliding have the valid status.
;If sprites doesnt have valid status then doesnt execute any of the routines in the vector.
;Always must have a RTS or RTL after call it and also after that the vector.
;Example of use:
;   JSL !Hitman_Base_NormalSpriteExecuteFullCollissionResultVector
;RTS
;
;Vector:
;dw MeNormalOtherNormal
;dw MeNormalOtherStationary
;dw MeNormalOtherKicked
;dw MeNormalOtherCarried
;dw MeStationaryOtherNormal
;dw MeStationaryOtherStationary
;dw MeStationaryOtherKicked
;dw MeStationaryOtherCarried
;dw MeKickedOtherNormal
;dw MeKickedOtherStationary
;dw MeKickedOtherKicked
;dw MeKickedOtherCarried
;dw MeCarriedOtherNormal
;dw MeCarriedOtherStationary
;dw MeCarriedOtherKicked
;dw MeCarriedOtherCarried

NormalSpriteExecuteFullCollissionResultVector:
    LDA !SpriteStatus,x
    BIT #$08
    BNE +
RTL
+
    LDA !SpriteStatus,y
    BIT #$08
    BNE +
RTL
+
    PHY
    AND #$03
    STA $F0
    LDA !SpriteStatus,x
    AND #$03
    ASL
    ASL
    ORA $F0
    INC A
    ASL
    TAY

    REP #$21
    LDA ($02,s),y
    STA $F3
    SEP #$20
    LDA $04,s
    STA $F5
    PLY

    JMP [$F3|!dp]
