; Set the geometry and response independently for each extended sprite number.
; Modes: 0 disabled, 1 ROM table, 2 byte box, 3 pose, 4 word box.
%SetExtendedHitboxSystem($05, $01)
%SetExtendedHitboxSystem($11, $01)
%SetExtendedFireballEvent($05, !HitmanEventFireball)
%SetExtendedFireballEvent($11, !HitmanEventYoshiFireball)
