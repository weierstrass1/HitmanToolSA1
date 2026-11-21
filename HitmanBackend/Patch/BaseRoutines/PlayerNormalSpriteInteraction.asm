PlayerNormalSpriteInteraction:
    STA !HitmanSprite1Context
    SEP #$20
	STA !HitmanNormalContextIDLow,x
	XBA
	STA !HitmanNormalContextIDHigh,x

	LDA !HitmanSprite1Flip
	STA !HitmanNormalFlip,x

    LDA !SpriteTweaker167A_DPMKSPIS,x
	AND #$20
	BNE .ProcessInteract      
	TXA                       
	EOR !TrueFrameCounter      			
	AND #$01                	
	ORA !SpriteHOffScreenFlag,x 				
	BEQ .ProcessInteract       
.ReturnNoContact
	CLC                       
RTL
.ProcessInteract

	JSL SubHorzPos
	LDA !ScratchF                  
	CLC                       
	ADC #$50                
	CMP #$A0                
	BCS .ReturnNoContact2       ; No contact, return 
	JSL SubVertPos    
	LDA !ScratchF                  
	CLC                       
	ADC #$60                
	CMP #$C0                
	BCS .ReturnNoContact2       					; No contact, return 
	LDA $71    									; \ If animation sequence activated... 
	CMP #$01                					;  | 
	BCS .ReturnNoContact2       					; / ...no contact, return 
	LDA #$00                					; \ Branch if bit 6 of $0D9B set 
	BIT $0D9B|!addr               				;  | 
	BVS +           							; / 
	LDA $13F9|!addr 							; \ If Mario and Sprite not on same side of scenery... 
	EOR !SpriteBehindEscenaryFlag,x 			;  |
+
	BNE .ReturnNoContact2

	JSL PlayerClippingRoutine

    JSL NormalSpritePositionSetup

    REP #$20
	LDA !HitmanSprite1Context
    JSL ProcessPlayerHitBoxes
RTL

.ReturnNoContact2
	CLC                       
RTL