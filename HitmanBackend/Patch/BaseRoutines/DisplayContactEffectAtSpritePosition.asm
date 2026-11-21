DisplayContactEffectAtSpritePosition:
	PHY
	LDA #$03
	STA $1DF9|!addr

	STZ $00
	STZ $01

	LDA #$08
	STA $02

	LDA #$02
	LDY #$03                ; \ find a free slot to display effect
	XBA
.loop
	LDA $17C0|!addr,y      ; |
	BEQ +                   ; |
	DEY                     ; |
	BPL .loop               ; |
	PLY
	SEC                     ; |
RTL                     	; /  RETURN if no slots open
+        
	XBA                     ; \ set effect graphic to smoke graphic
	STA $17C0|!addr,y      ; /
	LDA $02                 ; \ set time to show smoke
	STA $17CC|!addr,y      ; /

	LDA !SpriteYLow,x               ; \
	CLC                     ; | set smoke y position based on direction of shot
	ADC $01                 ; |
	STA $17C4|!addr,y      ; /

	LDA !SpriteXLow,x               ; \
	CLC                     ; | set smoke x position based on direction of shot
	ADC $00                 ; |
	STA $17C8|!addr,y      ; /

	PLY
RTL
