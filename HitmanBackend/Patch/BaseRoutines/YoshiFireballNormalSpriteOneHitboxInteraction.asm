YoshiFireballNormalSpriteOneHitboxInteraction:
	JSL NormalSpriteOneHitbox1

	LDY #!MaxExtended-1
.Loop
	LDA !ExtendedNumber,y
    CMP #$11
	BEQ .Next

	JSL YoshiFireballClipping

    JSL CheckContact
	BCC .Next
RTL
.Next
    DEY
    BPL .Loop
    CLC
RTL
