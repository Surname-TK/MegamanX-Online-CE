using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;
public class XBuster : Weapon {
	public static XBuster netWeapon = new();
	public List<BusterProj> lemonsOnField = new List<BusterProj>();
	public bool isUnpoBuster;

	public XBuster() : base() {
		index = (int)WeaponIds.Buster;
		killFeedIndex = 0;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 0;
		shootSounds = new string[] { "", "", "", "", "" };
		fireRate = 9;
		canHealAmmo = false;
		drawAmmo = false;
		drawCooldown = false;
		effect = "You only need this to win any match.";
		hitcooldown = "0/0/0/1";
		damage = "1/2/3/4";
		Flinch = "0/0/13/26";
		FlinchCD = "0";
	}
	public void setUnpoBuster(MegamanX mmx) {
		isUnpoBuster = true;
		fireRate = 45;
		weaponBarBaseIndex = 70;
		weaponBarIndex = 59;
		weaponSlotIndex = 121;
		killFeedIndex = 180;

		// Ammo variables
		maxAmmo = 28;
		ammo = maxAmmo;
		allowSmallBar = false;
		ammoGainMultiplier = 2;
		canHealAmmo = true;
		drawRoundedDown = true;
		
		// HUD.
		drawAmmo = true;
		drawCooldown = true;
		
		// Remove charge.
		mmx.stockedX2Charge = false;
		mmx.stockedX3Charge = false;
		mmx.stockedX3Saber = false;
	}

	public static bool isNormalBuster(Weapon weapon) {
		return weapon is XBuster buster && !buster.isUnpoBuster;
	}

	public static bool isWeaponUnpoBuster(Weapon weapon) {
		return weapon is XBuster buster && buster.isUnpoBuster;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if ((player.character as MegamanX)?.isHyperX == true && ammo < getAmmoUsage(chargeLevel)) {
			return false;
		}
		if (!base.canShoot(chargeLevel, player)) return false;
		if (chargeLevel > 1) {
			return true;
		}
		for (int i = lemonsOnField.Count - 1; i >= 0; i--) {
			if (lemonsOnField[i].destroyed) {
				lemonsOnField.RemoveAt(i);
				continue;
			}
		}
		
		return lemonsOnField.Count < 3;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		bool isUA = (character as MegamanX)?.hasUltimateArmor == true;
		string sound = "";

		if (chargeLevel == 0) {
			lemonsOnField.Add(new BusterProj(this, pos, xDir, 0, player, player.getNextActorNetId(), true));
			sound = "buster";
		} else if (chargeLevel == 1) {
			new Buster2Proj(this, pos, xDir, player, player.getNextActorNetId(), true);
			sound = "buster2";
		} else if (chargeLevel == 2) {
			new Buster3Proj(this, pos, xDir, 0, player, player.getNextActorNetId(), true);
			sound = "buster3";
		} else if (chargeLevel >= 3) {
			if (isUA) {
				new Anim(pos.clone(), "buster4_muzzle_flash", xDir, null, true);
				new BusterPlasmaProj(this, pos, xDir, player, player.getNextActorNetId(), true);
				sound = "plasmaShot";
			} else {
				new Anim(pos.clone(), "buster4_muzzle_flash", xDir, null, true);
				//Create the buster effect
				int xOff = xDir * -5;
				player.setNextActorNetId(player.getNextActorNetId());
				// Create first line instantly.
				createBuster4Line(pos.x + xOff, pos.y, xDir, player, 0f);
				// Create 2nd with a delay.
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					createBuster4Line(pos.x + xOff, pos.y, xDir, player, 10f / 60f);
					createBuster4Line(pos.x + xOff, pos.y, xDir, player, 10);
				}, 2.8f / 60f));
				// Use smooth spawn on the 3rd.
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					createBuster4Line(pos.x + xOff, pos.y, xDir, player, 5f / 60f, true);
					createBuster4Line(pos.x + xOff, pos.y, xDir, player, 5, true);
				}, 5.8f / 60f));
				sound = "buster4";
			}
		}

		if (!string.IsNullOrEmpty(sound)) character.playSound(sound, sendRpc: true);	
	}

	public override void shootGiga(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		string sound = "";

		if (chargeLevel == 0) {
			lemonsOnField.Add(new BusterProj(this, pos, xDir, 0, player, player.getNextActorNetId(), true));
			sound = "buster";
		} else if (chargeLevel == 1) {
			new Buster2Proj(this, pos, xDir, player, player.getNextActorNetId(), true);
			sound = "buster2";
		} else if (chargeLevel == 2) {
			new Buster3Proj(this, pos, xDir, 0, player, player.getNextActorNetId(), true);
			sound = "buster3";
		} else if (chargeLevel == 3) {	
			if (player.ownedByLocalPlayer) {
				if (mmx.hasUltimateArmor && !mmx.stockedX2Charge) {
					if (mmx.charState is not WallSlide) {
						mmx.changeState(new X2ChargeShot(3), true);
					} else {
						mmx.gigaArmorChargeShots(3);
					}
				} else {
					int type = mmx.stockedX2Charge ? 2 : 0;

					if (character.charState is not WallSlide) {
						character.changeState(new X2ChargeShot(type), true);
						mmx.shootCooldown = 0;
					} else {
						mmx.gigaArmorChargeShots(type);
					}	
				}
			}
		}  else if (chargeLevel >= 4) {	
			if (player.ownedByLocalPlayer) {
				if (mmx.hasUltimateArmor && !mmx.stockedX2Charge) {
					if (mmx.charState is not WallSlide) {
						mmx.changeState(new X2ChargeShot(3), true);
					} else {
						mmx.gigaArmorChargeShots(3);
					}
				} else {
					int type = mmx.stockedX2Charge ? 2 : 1;

					if (character.charState is not WallSlide) {
						character.changeState(new X2ChargeShot(type), true);
						mmx.shootCooldown = 0;
					} else {
						mmx.gigaArmorChargeShots(type);
					}	
				}
			}
		}

		if (!string.IsNullOrEmpty(sound)) character.playSound(sound, sendRpc: true);
	}

	public override void shootMax(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		string sound = "";

		if (mmx.stockedX3Charge || mmx.stockedX2Charge || mmx.stockedLv1Charge) {
			int type = mmx.stockedLv1Charge ? 2 : 3;
			if (mmx.charState is not WallSlide) {
				mmx.changeState(new X3ChargeShot(null) {state = type}, true);
			} else {
				mmx.maxArmorChargeShots(type, null!);
			}
			
		} else if (chargeLevel == 0) {
			lemonsOnField.Add(new BusterProj(this, pos, xDir, 0, player, player.getNextActorNetId(), true));
			sound = "buster";
		} else if (chargeLevel == 1) {
			new Buster2Proj(this, pos, xDir, player, player.getNextActorNetId(), true);
			sound = "buster2";
		} else if (chargeLevel == 2) {
			new Buster3Proj(this, pos, xDir, 0, player, player.getNextActorNetId(), true);
			sound = "buster3";
		} else if (chargeLevel == 3) {
			if (player.ownedByLocalPlayer) {
				if (character.charState is not WallSlide) {
					character.changeState(new X3ChargeShot(null) {state = 0}, true);
					mmx.shootCooldown = 0;
				} else {
					mmx.maxArmorChargeShots(0, null!);
				}
			}
		} else if (chargeLevel >= 4) {
			if (player.ownedByLocalPlayer) {
				if (character.charState is not WallSlide) {
					character.changeState(new X3ChargeShot(null) {state = 1}, true);
					mmx.shootCooldown = 0;
				} else {
					mmx.maxArmorChargeShots(1, null!);
				}
			}
		}

		if (!string.IsNullOrEmpty(sound)) character.playSound(sound, sendRpc: true);
	}
	public void createBuster4Line(
		float x, float y, int xDir, Player player,
		float offsetTime, bool smoothStart = false
	) {
		new Buster4Proj(
			this, new Point(x + xDir, y), xDir,
			player, 0, offsetTime,
			player.getNextActorNetId(allowNonMainPlayer: true), smoothStart, true
		);
		Global.level.delayedActions.Add(new DelayedAction(delegate {
			new Buster4Proj(
				this, new Point(x + xDir, y), xDir,
				player, 1, offsetTime,
				player.getNextActorNetId(allowNonMainPlayer: true), smoothStart, true
			);
		}, 1.8f / 60f
		));
		Global.level.delayedActions.Add(new DelayedAction(delegate {
			new Buster4Proj(
				this, new Point(x + xDir, y), xDir,
				player, 2, offsetTime,
				player.getNextActorNetId(allowNonMainPlayer: true), smoothStart, true
			);
		}, 3.8f / 60f
		));
		Global.level.delayedActions.Add(new DelayedAction(delegate {
			new Buster4Proj(
				this, new Point(x + xDir, y), xDir,
				player, 2, offsetTime,
				player.getNextActorNetId(allowNonMainPlayer: true), smoothStart, true
			);
		}, 5.8f / 60f
		));
		Global.level.delayedActions.Add(new DelayedAction(delegate {
			new Buster4Proj(
				this, new Point(x + xDir, y), xDir,
				player, 3, offsetTime,
				player.getNextActorNetId(allowNonMainPlayer: true), smoothStart, true
			);
		}, 7.8f / 60f
		));
	}

	public override void shootStock(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		bool isUA = mmx.hasUltimateArmor;
		string sound = "";

		if (mmx.forceStocks >= 1) {
			new BusterStockProj(this, pos, xDir, player, player.getNextActorNetId());
			mmx.forceStocks--;
			sound = "buster2";
		} else {
			BusterProj lemon = new BusterProj(
				this, pos, xDir, 0, player, player.getNextActorNetId());
			lemonsOnField.Add(lemon);
			sound = "buster";
		}

		if (!string.IsNullOrEmpty(sound)) character.playSound(sound, sendRpc: true);	
	}

	public override void shootPlasma(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		bool isUA = mmx.hasUltimateArmor;
		string sound = "";

		if (chargeLevel >= 3) {
				new BusterForcePlasmaProj(this, pos, xDir, player, player.getNextActorNetId());
				new Anim(pos, "buster_plasma_muzzle", xDir, null, true);
				sound = "plasmaShot";
			} else if (chargeLevel == 2) {
				new ForceBuster3Proj(this, pos, xDir, player, player.getNextActorNetId());
				new Anim(pos.addxy(player.character.xDir + 2, 0), "buster4_x3_muzzle", xDir, null, destroyOnEnd: true);
				sound = "buster3";
			} else if (chargeLevel == 1) {
				new Buster2Proj(this, pos, xDir, player, player.getNextActorNetId());
				sound = "buster2";
			} else {
				BusterProj lemon = new BusterProj(
					this, pos, xDir, 0, player, player.getNextActorNetId());
				lemonsOnField.Add(lemon);
				sound = "buster";
			}

		if (!string.IsNullOrEmpty(sound)) character.playSound(sound, sendRpc: true);	
	}
}
