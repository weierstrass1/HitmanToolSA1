CheckContact:
	REP #$20
.checkXAxis

	LDA !HitmanHB1Right
	CMP !HitmanHB2Left
	BCC .End			     ;if HB 2 is at the left of HB 1

	LDA !HitmanHB2Right
	CMP !HitmanHB1Left
	BCC .End			     ;if HB 2 is at the right of HB 1

.checkYAxis

	LDA !HitmanHB1Bottom
	CMP !HitmanHB2Top
	BCC .End			     ;if HB 2 is at the left of HB 1

	LDA !HitmanHB2Bottom
	CMP !HitmanHB1Top
	BCC .End			     ;if HB 2 is at the right of HB 1
.End
	SEP #$20
RTL