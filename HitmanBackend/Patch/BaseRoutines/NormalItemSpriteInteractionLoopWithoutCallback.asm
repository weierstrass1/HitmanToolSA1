NormalItemSpriteInteractionLoopWithoutCallback:
	STA !HitmanSprite1Context
	SEP #$20
	STA !HitmanNormalContextIDLow,x
	XBA
	STA !HitmanNormalContextIDHigh,x
	
	LDA !HitmanSprite1Flip
	STA !HitmanNormalFlip,x

	JSL NormalSpritePositionSetup

	PHB
	PHK
	PLB

	LDX.b #!MaxSprites-1
.Loop
	CPX !SpriteIndex
	BEQ .next
	LDA !SpriteStatus,x
	CMP #$09
	BNE +
	LDA !SpriteXSpeed,x
	ORA !SpriteYSpeed,x
	BEQ .next
	BRA .start
+
	TAY
	LDA .CanInteract,y
	BEQ .next

.start
	LDA !SpriteTweaker1686_DNCTSWYE,x
	AND !Hitman_Prop_HitboxProperties,x
	AND #$08
	BNE .next

	JSL AnyNormalSpriteInteractionWithoutCallback
	BCC .next

	PLB
	TXY
	LDX !SpriteIndex
RTL

.next

	DEX
	BPL .Loop

	PLB
	LDX !SpriteIndex
	CLC
RTL

.CanInteract
	db $00	;Empty slot.
	db $00	;Slot taken, but INIT has not yet been called.
	db $00	;Killed and falling off screen.
	db $00	;Killed by smushing. (e.g. shell-less Koopa, P-switch)
	db $00	;Killed with a spinjump.
	db $00	;Killed by lava/mud.
	db $00	;Turned into a coin by the goal tape.
	db $00	;Inside Yoshi's mouth.
	db $00	;Normal routine.
	db $01	;Stationary / Carryable.
	db $01	;Kicked.
	db $01	;Being carried.
	db $00	;Turned into a powerup by the goal tape.
