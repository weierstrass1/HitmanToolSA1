ItemCheckBounce:
	LDA $65
	BPL .ProcessItem
	JSL CheckBounce
RTL
.ProcessItem
	LDA !SpriteStatus,x
	CMP #$09
	BNE +
	LDA #$01
	TSB $4F
RTL
+
	JSL CheckBounce
RTL
