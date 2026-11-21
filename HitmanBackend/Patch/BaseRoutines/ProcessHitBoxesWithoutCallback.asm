;Input:
;A = Context ID (16 bits)
;!HitmanSprite1Flip = Flip
;   0 => no flip
;   1 => H flip
;   2 => V flip
;   3 => HV flip
;!HitmanSprite1X = Sprite X Position
;!HitmanSprite1Y = Sprite Y Position
;!HitmanHB2Left = Left Side of the other Hitbox 
;!HitmanHB2Top = Top Side of the other Hitbox
;!HitmanHB2Right = Right Side of the other hitbox
;!HitmanHB2Bottom = Bottom Side of the other hitbox

;Output:
;Carry set if a hitbox collides
;Y = Hitbox ID that collided

;!HitmanHB1Left = Left Side of the Hitbox that Collided
;!HitmanHB1Top = Top Side of the Hitbox that Collided
;!HitmanHB1Right = Right Side of the hitbox that Collided
;!HitmanHB1Bottom = Bottom Side of the hitbox that Collided
ProcessHitBoxesWithoutCallback:
    PHB
    ASL
    PHA
    SEP #$30
    
    LDA.b #ContextPosesHitboxes>>16
    PHA
    PLB

    LDA !HitmanSprite1Flip
    ASL
    TAY

    REP #$31
    LDA.w HitBoxXOffsetTables,y
    STA !HitmanHitboxXOffset
    LDA.w HitBoxYOffsetTables,y
    STA !HitmanHitboxYOffset
    
    PLY

    LDA.w ContextPosesHitboxes,y
    TAY

    LDA.w PoseHitboxesIndexers,y
    TAY

.Loop
    LDA.w PoseHitboxes,y
    CMP #$FFFF
    BNE ..ValidHitbox
    SEP #$30

    CLC
    PLB
RTL
..ValidHitbox
    PHY
    TAY

    LDA (!HitmanHitboxXOffset),y
    CLC
    ADC !HitmanSprite1X
    STA !HitmanHB1Left
    BPL +
    STZ !HitmanHB1Left
+
    CLC
    ADC.w HitboxWidths,y
    STA !HitmanHB1Right
    BPL +
    LDA #$FF00
    STA !HitmanHB1Left
    STA !HitmanHB1Right
+

    LDA (!HitmanHitboxYOffset),y
    CLC
    ADC !HitmanSprite1Y
    STA !HitmanHB1Top
    BPL +
    STZ !HitmanHB1Top
+
    CLC
    ADC.w HitboxHeights,y
    STA !HitmanHB1Bottom
    BPL +
    LDA #$FF00
    STA !HitmanHB1Top
    STA !HitmanHB1Bottom
+

..checkXAxis

	LDA !HitmanHB1Right
	CMP !HitmanHB2Left
	BCC ..Next			     ;if HB 2 is at the left of HB 1

	LDA !HitmanHB2Right
	CMP !HitmanHB1Left
	BCC ..Next			     ;if HB 2 is at the right of HB 1

..checkYAxis

	LDA !HitmanHB1Bottom
	CMP !HitmanHB2Top
	BCC ..Next			     ;if HB 2 is at the left of HB 1

	LDA !HitmanHB2Bottom
	CMP !HitmanHB1Top
	BCC ..Next			     ;if HB 2 is at the right of HB 1

..contact				     ;HB 2 is touching HB 1
    PLA
    LSR
    SEP #$30
    TAY

    SEC
    PLB
RTL

..Next
    PLY
    INY
    INY
    BRA .Loop
