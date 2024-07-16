public enum enWorkspaceType
{
    External,
    Owned,
    Master
}

public enum organization
{
    BBC,
    SKY,
}

public enum enUploadTrackStatus
{
    Created = 1,
    Upload_Success = 2,
    Upload_Failed = 3
}

public enum enMLStatus : byte
{
    Available = 1,
    Live = 2,
    Archive = 3,
    Restrict = 4,
    Available_Loked = 5
}

public enum enCtagStatus : byte
{
    Active = 1,
    Archived = 2,
}

public enum enCTagTypes : byte
{
    BBC_RESTRICTED_ARTIST = 1,
    PRS_PRIOR_APPROVAL_CODE = 2,
    PRS_PUBLIC_DOMAIN = 3,
    PRS_MCPS_OWNERSHIP = 4,
    MCPS_MUSIC = 5,
    NORTH_AMERICAN_COPYRIGHT = 6,
    PPL_LABEL = 7
}

public enum enDHStatus : byte
{
    Synced = 1,
    Available = 2,
    Live = 3
}

public enum enLibWSDownloadStatus : byte
{
    ToBeDownload = 1,
    Inprogress = 2,
    DownloadSuccess = 3,
    DownloadFailed = 4,
    Pause = 5
}

public enum enSyncStatus : byte
{
    ToBeSynced = 1,
    Inprogress = 2,
    SyncSuccess = 3,
    SyncFailed = 4
}

public enum enIndexStatus : byte
{
    ToBeIndexed = 1,
    Inprogress = 2,
    IndexSuccess = 3,
    IndexFailed = 4
}

public enum enWorkspaceLib
{
    ws,
    lib
}

public enum enTakedownType
{
    DH,
    EXP,
    USR,
    CTAG
}

public enum enPreReleaseType
{
    DH,
    EXP,
    USR
}

public enum enWorkspaceAction
{
    SET_AVL,
    SET_AVL_LOCKED,
    SET_LIVE,
    SET_ALIVE,
    TAKEDOWN,
    UNDO_TAKEDOWN,
    ARCHIVE_TRACK,
    UNDO_ARCHIVE_TRACK,
    RESTRICT,
    UNDO_RESTRICT,
    SET_BBC_OWNED,
    REMOVE_BBC_OWNED,
    SET_BBC_EXCLUDE,
    REMOVE_BBC_EXCLUDE,
    RESYNC,
    PAUSE,
    CONTINUE,
    RESTRICT_TRACK,
    REMOVE_RESTRICT_TRACK,
    UPDATE_MUSIC_ORIGIN
}

public enum enActionType : byte
{
    SaveWorkspace = 1,
    SET_LIVE = 2,
    SET_ALIVE = 3,
    TAKEDOWN = 4,
    ARCHIVE_TRACK = 5,
    RESTRICT = 6,
    SET_BBC_OWNED = 7,
    SET_BBC_EXCLUDE = 8,
    REMOVE_BBC_EXCLUDE = 9,
    RESYNC = 10,
    PAUSE = 11,
    CONTINUE = 12,
    RESTRICT_TRACK = 13,
    REMOVE_RESTRICT_TRACK = 14,
    SET_AVL = 15,
    COPY_TRACK = 16,
    UPDATE_TRACK = 17,
    REMOVE_TRACK = 18,
    EXCEPTION = 19,
    UPLOAD_TRACK = 20,
    SAVE_ALBUM = 21,
    UPDATE_ALBUM = 22,
    DELETE_ALBUM = 23,
    ADD_TRACK_TO_ALBUM = 24,
    ADD_CTAG = 25,
    UPDATE_CTAG = 26,
    ADD_CTAG_RULE = 27,
    UPDATE_CTAG_RULE = 28,
    DELETE_CTAG_RULE = 29,
    CHECK_CTAG_RULE = 30,
    CREATE_PLAYOUT = 31,
    CREATE_PPLLABEL = 32,
    UPDATE_PPLLABEL = 33,
    CREATE_PRIOR_APPROVAL = 34,
    UPDATE_PRIOR_APPROVAL = 35,
    PUBLISH_PLAYOUT = 36,
    ADD_TO_PLAYOUT = 37,
    CONTENT_ALERT = 38,
    CONTENT_ALERT_RESOLVE = 39,
}
public enum enLogStatus : byte
{
    Success = 1,
    Failed = 0
}

public enum enAdminTypes
{
    BBC_ADMIN_NOTES,
    BBC_ADMIN_TAG,
    BBC_USER_TAG,
    BBC_TRACK_ID,
    BBC_ALBUM_ID,
    SUB_ORIGIN
}

public enum enContributorsExtended
{
    choir,
    conductor,
    ensemble,
    featured_artist,
    orchestra,
    remix_artist,
    versus_artist
}

public enum enMLUploadAction
{
    ML_TRACK_COPY,
    ML_ALBUM_COPY,
    ML_TRACK_ADD,
    ML_ALBUM_ADD
}

public enum enDescriptiveExtendedSource
{
    ML_COPY,
    ML_UPLOAD,
    BBC_FIELDS
}

public enum enDescriptiveExtendedType
{
    copy_source_track_id,
    upload_track_id,
    copy_source_album_id,
    upload_album_id,
    bbc_admin_notes,    
    bbc_track_id,
    bbc_album_id
}

public enum enTrackChangeLogAction
{
    COPY,
    UPLOAD,
    EDIT
}

public enum enAlbumChangeLogAction
{
    UPLOAD,
    EDIT,
    COPY
}

public enum enUploadRecType
{
    COPY,
    UPLOAD,
    CREATE
}

public enum enPrsSearchType
{
    TUNECODE,
    ISRC,
    TITLE_PERFORMER
}
public enum enPrsQueryParam
{
    PRS,
    ISRC,
    TITLE_PERFORMER
}

public enum enQueue
{
    ML_PLAYOUT
}

public enum enCtagRuleStatus
{
    Active = 1,
    Archive = 2,
    Created = 3
}

public enum enPlayoutStatus : byte
{
    CREATED = 1,
    PUBLISHED = 2,
    ERROR = 3,
    COMPLETED = 4,
    ARCHIVED = 5,
    PUBLISH_FAILED = 6
}

public enum enPlayoutTrackStatus : int
{
    CREATED = 1,
    TO_BE_PUBLISHED = 2,
    PUBLISHED = 3,
    PUBLISH_FAILED = 4,
}

public enum enPlayoutAction : byte
{
    CREATE = 1,
    CREATE_AND_PUBLISH = 2,
    PUBLISH = 3
}

public enum enPlayoutSessionPublishStatus : int
{
    PENDING = 0,
    CREATE = 1,
    REPUBLISH = 2,
    INPROGRESS = 3,   
    PUBLISH_DONE = 4,
    PUBLISH_FAILED = 5,
    SIGNIANT_RUNNING = 6,
    SIGNIANT_SUCCESS = 7,
    SIGNIANT_FAILED = 8,
    ARCHIVE = 9
}

public enum enPlayoutTrackXMLStatus : int
{
    TO_BE_CREATED = 1,
    CREATED = 2,
    CREATE_FAILED = 3,
    UPLOADED = 4,    
    UPLOAD_FAILED = 5,
}

public enum enPlayoutTrackAssetStatus : int
{
    ASSET_NOT_FOUND = 0,
    TO_BE_DOWNLOADED = 1,
    DOWNLOADED = 2,
    DOWNLOAD_FAILED = 3,
    UPLOADED = 4,
    UPLOAD_FAILED = 5,
}

public enum enIPRole
{
    arranger,
    composer,
    performer,
    lyricist,
    publisher,
    translator,
    record_label,
    adaptor,
    administrator,
    original_publisher,
    sub_lyricist,
    sub_adaptor,
    sub_arranger,
    sub_publisher,
    composer_lyricist
}

public enum enDHMusicOrigin
{
    commercial,
    commissioned,
    library,
    library_non_mechanical,
    library_non_affiliated,
    live
}

public enum enMLMusicOrigin
{
    commercial,
    commissioned,
    library,   
    live
}

public enum enPlayoutErrorCodes
{
    THREE_HOURS_SINGLE = 51,
    TEN_HOURS_ALL = 99,
    ASSET_NOT_FOUND = 100,
    MAX_TRACK_COUNT_EXEED = 59
}

public enum enServiceType
{
    Playout_service = 2,
    Sync_Master_Workspace = 6,
    Sync_External_Workspace = 3,
    Track_Upload_Service = 9,
    Workspace_Library_Sync_Service = 1,
    Index_Ctag_Service = 4,
    PRS_Index_Service = 10,
    Takedown_Service = 11,
    Set_Searchable_Service = 12,
    Set_Pre_Release_Service = 13,
}

public enum enServiceStatus
{
    pass,
    fail
}

public enum enPlayoutDownloadType
{
    XML,
    ZIP
}










