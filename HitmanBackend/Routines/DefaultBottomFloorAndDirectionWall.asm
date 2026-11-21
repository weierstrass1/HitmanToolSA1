DefaultBottomFloorAndDirectionWall:
    JSL PlayerFloor
    BCC +
RTL
+
    LDA !SpriteDirection,x
    BNE +
    JSL PlayerLeftWall
    BCC ++
    LDA #$08
    TSB !HitmanDefault
RTL
++
    LDA #$04
    TSB !HitmanDefault
RTL
+
    JSL PlayerRightWall
    BCC +
    LDA #$08
    TSB !HitmanDefault
RTL
+
    LDA #$04
    TSB !HitmanDefault
RTL
