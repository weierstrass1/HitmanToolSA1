;This stun a kicked or stationary sprite
;It is a simplified version of vanilla routine
;This doesnt spawn contact effect, that must be displayed from the sprite code
;A = Y Speed (Recommended #$C0 or #$B0)
StunKickedSprite:
    STA !SpriteYSpeed,x

	STZ !SpriteDecTimer3,x

    LDA #$00
	JSL $02ACE5|!rom

	LDA #$09                                    ; \ Sprite status = Carryable
	STA !SpriteStatus,x                         ; /
	ASL !SpriteProperties_YXPPCCCT,x
	SEC
	ROR !SpriteProperties_YXPPCCCT,x

	JSL SubHorzPos
	LDA .QuakeSpriteStompSpeed,y
    STA !SpriteXSpeed,x
	TYA
    EOR #$01
    STA !SpriteDirection,X
RTL
.QuakeSpriteStompSpeed:
	db $F8,$08
