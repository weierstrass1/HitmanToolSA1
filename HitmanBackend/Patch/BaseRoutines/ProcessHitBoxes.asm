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
;Carry set if a hitbox changed 4F to non zero
;4F can be used as a result state.
macro HitmanProcessHitBoxes(contact, noContact)
    PHB
    ASL
    PHA
    SEP #$30

    STZ !HitmanDefault
    
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

    LDA !HitmanDefault
    CMP #$01
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
	BCC .left			        ;if HB 2 is at the left of HB 1

	LDA !HitmanHB2Right
	CMP !HitmanHB1Left
	BCC .right			        ;if HB 2 is at the right of HB 1

.touchingHorizontal		    ;HB2 is touching HB1 in X axys
	LDA #$0003
	BRA +
.left
	LDA #$0002
	BRA +
.right
	LDA #$0001
+
	STA !HitmanHBStatus

..checkYAxis

	LDA !HitmanHB1Bottom
	CMP !HitmanHB2Top
	BCC .up			        ;if HB 2 is at the left of HB 1

	LDA !HitmanHB2Bottom
	CMP !HitmanHB1Top
	BCC .down			    ;if HB 2 is at the right of HB 1

.touchingVertical		    ;HB 2 is touching HB2 in Y axys
	LDA #$000C
	BRA +
.up
	LDA #$0008
	BRA +
.down
	LDA #$0004
+
	TSB !HitmanHBStatus

    ; The saved pose entry indexes the parallel callback tables.
    LDA $01,s
    TAY
    LDA.w PoseHitboxCallbackIds,y
    PHA
    LDA.w PoseHitboxProfiles,y
    TAY
.contact				;HB 2 is touching HB 1
	LDA !HitmanHBStatus
	CMP #$000F
	BNE .ThereIsntContact

.ThereIsContact

	LDA.w <contact>,y
	TAY 

	BRA .execRoutine

.ThereIsntContact

	LDA.w <noContact>,y
	TAY

.execRoutine
	LDA.w HitboxActions,y
	STA !HitmanRoutineAddress
	LDA.w HitboxActions+1,y
	STA !HitmanRoutineAddress+1

	PLA
	SEP #$30
	TAY						;Y = Hitbox ID

    PHB
	PHK
    PLB
    PHK
	LDA.b #(..PreNext-1)>>8
	PHA
	LDA.b #(..PreNext-1)
	PHA
	JML [!HitmanRoutineAddress|!dp]
..PreNext
    PLB

..Next
    REP #$30
    PLY
    INY
    INY
    JMP .Loop

endmacro

ProcessHitBoxes:
    %HitmanProcessHitBoxes(HitboxActions1, HitboxActions2)

ProcessPlayerHitBoxes:
    %HitmanProcessHitBoxes(HitboxPlayerActions1, HitboxPlayerActions2)
