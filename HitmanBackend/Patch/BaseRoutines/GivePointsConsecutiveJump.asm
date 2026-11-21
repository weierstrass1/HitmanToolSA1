GivePointsConsecutiveJump:
	PHX
	INC $1697|!addr
	LDX $1697|!addr
	CPX #$08
	BCC +
	LDX #$08
	STX $1697|!addr
+
	LDA.l .StompSFX-1,x
	STA $1DF9|!addr 	;Play sound effect
	TXA
	PLX
	JSL $02ACE5|!rom
RTL

.StompSFX:
	db $13,$14,$15,$16,$17,$18,$19,$02,$02,$02
