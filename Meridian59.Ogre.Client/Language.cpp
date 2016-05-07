#include "stdafx.h"

/**************************************************************************************/
/***************************      ENGLISH     *****************************************/
/**************************************************************************************/

const char* EN[] =
{
	"Username",				// 0
	"Password",				// 1
	"Connect",				// 2
	"Options",				// 3
};

/**************************************************************************************/
/***************************      GERMAN      *****************************************/
/**************************************************************************************/

const char* DE[] =
{
	"Benutzername",			// 0
	"Passwort",				// 1
	"Verbinden",			// 2
	"Optionen",				// 3
};

/**************************************************************************************/
/**************************************************************************************/

const char* GETSTR(const LANGSTR ID)
{
	using ::Meridian59::Ogre::OgreClient;
	using ::Meridian59::Common::Enums::LanguageCode;

	switch (OgreClient::Singleton->Config->Language)
	{
		case LanguageCode::German:	return DE[ID];
		default:					return EN[ID];
	}
};
