ItemCheckPlayerIsAbove:
	LDA !SpriteStatus,x
	CMP #$09
	BEQ +
	JSL CheckPlayerIsAbove
+
RTL