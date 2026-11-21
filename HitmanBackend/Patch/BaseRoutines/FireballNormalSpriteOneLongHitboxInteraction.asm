FireballNormalSpriteOneLongHitboxInteraction:
	JSL NormalSpriteLongOneHitbox1

.Slot0
	LDY #!MaxExtended-1
	LDA !ExtendedNumber,y
	BEQ .Slot1

	JSL FireballClipping

    JSL CheckContact
	BCC .Slot1
RTL
.Slot1
	LDY #!MaxExtended-2
	LDA !ExtendedNumber,y
	BEQ +

	JSL FireballClipping

    JSL CheckContact
	BCC .Slot1
RTL
+
	CLC
RTL
