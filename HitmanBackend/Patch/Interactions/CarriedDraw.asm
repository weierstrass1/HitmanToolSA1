; Entry: M=X=1, X=sprite slot. Carry handling has already run.
org $019F74|!rom
    autoclean JML HitmanCarriedDraw
    NOP

freecode
HitmanCarriedDraw:
    LDA !Hitman_Prop_SkipVanillaCarriedDraw,x
    BEQ .Vanilla
    JML $019F98|!rom

.Vanilla
    LDA $13DD|!addr
    BEQ .Continue
    JML $019F83|!rom
.Continue
    JML $019F79|!rom
