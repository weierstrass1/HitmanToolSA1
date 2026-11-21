;Can execute a collission result from the vector if the other sprite that is 
;colliding have the valid status.
;If sprite doesnt have valid status then doesnt execute any of the routines in the vector.
;Always must have a RTS or RTL after call it and also after that the vector.
;Example of use:
;   JSL !Hitman_Base_NormalSpriteExecuteCollissionResultVector
;RTS
;
;Vector:
;dw OtherNormal
;dw OtherStationary
;dw OtherKicked
;dw OtherCarried

NormalSpriteExecuteCollissionResultVector:
    LDA !SpriteStatus,y
    BIT #$08
    BNE +
RTL
+
    PHY
    AND #$03
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

    JML [$F3|!dp]
