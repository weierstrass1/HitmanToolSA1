CheckBounceCluster:	

	LDA #$01
	TSB !HitmanDefault

	REP #$20

	LDA !HitmanHB1Top
	CMP $6A
	BCS +
	STA $6A

	LDA !HitmanHB1Left
	STA $6C
	LDA !HitmanHB1Right
	STA $6E
	LDA !HitmanHB1Bottom
	STA $1487|!addr

+
	SEP #$20
	
	LDA !HitmanClusterPlayerIsAbove,x
	BMI +
	BEQ +
	LDA #$08
	STA !HitmanClusterPlayerIsAbove,x
RTL
+
	REP #$20
	LDA !HitmanHB1Top
	SEC
	SBC !HitmanHB2Bottom
	BPL +
	EOR #$FFFF
	INC A
+
	CMP #$0008
	SEP #$20
	BCC +
RTL
+
	LDA !PlayerYSpeed
	PHA
	LDA !PlayerBlockedStatus_S00MUDLR
	AND #$04
	BEQ +
	LDA #$00
	STA $01,s
+

	LDA $01,s
	BMI +
	LDA !ClusterMiscTable7,x
	BMI .ValidHit
+
	LDA !ClusterMiscTable7,x
	SEC
	SBC $01,s
	BMI .ValidHit
	PLA
RTL
.ValidHit
	PLA

	LDA #$08
	STA !HitmanClusterPlayerIsAbove,x

	REP #$20
	LDA !HitmanHB2Bottom
	CMP $6A
	BCC +
	STA $6A
+
	SEP #$20
RTL