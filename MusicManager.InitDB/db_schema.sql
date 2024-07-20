--
-- PostgreSQL database dump
--

-- Dumped from database version 12.9 (Ubuntu 12.9-1.pgdg18.04+1)
-- Dumped by pg_dump version 12.9 (Ubuntu 12.9-1.pgdg18.04+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: charts; Type: SCHEMA; Schema: -; Owner: admin
--

CREATE SCHEMA charts;


ALTER SCHEMA charts OWNER TO admin;

--
-- Name: log; Type: SCHEMA; Schema: -; Owner: admin
--

CREATE SCHEMA log;


ALTER SCHEMA log OWNER TO admin;

--
-- Name: playout; Type: SCHEMA; Schema: -; Owner: admin
--

CREATE SCHEMA playout;


ALTER SCHEMA playout OWNER TO admin;

--
-- Name: staging; Type: SCHEMA; Schema: -; Owner: admin
--

CREATE SCHEMA staging;


ALTER SCHEMA staging OWNER TO admin;

--
-- Name: fn_search_elastic_album_changes(integer, uuid); Type: FUNCTION; Schema: log; Owner: admin
--

CREATE FUNCTION log.fn_search_elastic_album_changes(pagesize integer, orgworkspaceid uuid) RETURNS TABLE(document_id uuid, album_id uuid, metadata json, album_org_data json, workspace_id uuid, library_id uuid, workspace_name character varying, library_name character varying, library_notes character varying, restricted boolean, archived boolean, deleted boolean, ws_type character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
	RETURN QUERY select eac.document_id,mma.album_id,mma.metadata,eac.album_org_data,mma.workspace_id,mma.library_id,w.workspace_name,
 l.library_name,l.notes as library_notes,eac.restricted,eac.archived,mma.archived as deleted,
 COALESCE(ow.ws_type, "left"('External'::text, 10)::character varying) AS ws_type
from log.elastic_album_change eac 
left join public.ml_master_album mma on eac.original_album_id = mma.album_id 
left join public.workspace w on mma.workspace_id = w.workspace_id
left join org_workspace ow ON w.workspace_id = ow.dh_ws_id
left join public.library l  on mma.library_id = l.library_id 
where eac.org_workspace_id = orgworkspaceid
LIMIT pagesize;
	
END;
$$;


ALTER FUNCTION log.fn_search_elastic_album_changes(pagesize integer, orgworkspaceid uuid) OWNER TO admin;

--
-- Name: fn_search_elastic_track_changes(integer, integer, uuid); Type: FUNCTION; Schema: log; Owner: admin
--

CREATE FUNCTION log.fn_search_elastic_track_changes(pageno integer, pagesize integer, orgworkspaceid uuid) RETURNS TABLE(id bigint, document_id uuid, track_org_data json, dh_version_id uuid, metadata json, date_created timestamp without time zone, workspace_name character varying, library_name character varying, external_identifiers json, deleted boolean, source_ref character varying, ext_sys_ref character varying, ws_type character varying, archived boolean, edit_track_metadata json, edit_album_metadata json, pre_release boolean, org_id character varying, restricted boolean, album_metadata json, album_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
	--IF pageNo = 0 THEN
	--	totalCount := (select count(*) from log.log_elastic_track_changes where processed = false);
	--END IF;
	
	RETURN QUERY select etc.id,etc.document_id,etc.track_org_data,mmt.dh_version_id,mmt.metadata,etc.date_created,w.workspace_name,l.library_name,
mmt.external_identifiers,etc.deleted,mmt.source_ref,mmt.ext_sys_ref,COALESCE(ow.ws_type, "left"('External'::text, 10)::character varying) AS ws_type,
etc.archived,mmt.edit_track_metadata,mmt.edit_album_metadata,mmt.pre_release,etc.org_id,etc.restricted,mma.metadata as album_metadata,mma.album_id
from log.elastic_track_change etc
left join public.ml_master_track mmt on etc.original_track_id = mmt.track_id 
left join public.workspace w on mmt.workspace_id = w.workspace_id 
left join org_workspace ow ON w.workspace_id = ow.dh_ws_id
left join public.library l  on mmt.library_id = l.library_id 
left join ml_master_album mma on mmt.album_id = mma.album_id 
where etc.org_workspace_id = orgworkspaceid
order by etc.id
LIMIT pagesize;

END;
$$;


ALTER FUNCTION log.fn_search_elastic_track_changes(pageno integer, pagesize integer, orgworkspaceid uuid) OWNER TO admin;

--
-- Name: fn_lib_change_trigger(); Type: FUNCTION; Schema: public; Owner: admin
--

CREATE FUNCTION public.fn_lib_change_trigger() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE _status_name text DEFAULT null;
	DECLARE _old_val text DEFAULT null;
	DECLARE _new_val text DEFAULT null;

BEGIN	
	
	IF OLD.dh_status <> NEW.dh_status THEN
		_status_name := 'dh';
		_old_val := OLD.dh_status;
		_new_val := NEW.dh_status;
		
		INSERT INTO log.log_ws_lib_status_change(record_type,record_id,old_status,new_status,date_created,created_by,status_name)
    	VALUES ('l',OLD.library_id,_old_val,_new_val,CURRENT_TIMESTAMP,NEW.last_edited_by,_status_name); 
	END IF;
	
	IF OLD.ml_status <> NEW.ml_status THEN
		_status_name := 'ml';
		_old_val := OLD.ml_status;
		_new_val := NEW.ml_status;
		
		INSERT INTO log.log_ws_lib_status_change(record_type,record_id,old_status,new_status,date_created,created_by,status_name)
    	VALUES ('l',OLD.library_id,_old_val,_new_val,CURRENT_TIMESTAMP,NEW.last_edited_by,_status_name); 
		
		IF (NEW.ml_status = 'LIVE' or NEW.ml_status = 'ALIVE') THEN
			IF (SELECT count(*) FROM public.ws_lib_tracks_to_be_synced WHERE ref_id=OLD.workspace_id and type='w') = 0 and 
			(SELECT count(*) FROM public.ws_lib_tracks_to_be_synced WHERE ref_id=OLD.library_id and type='l') = 0 THEN
		 		INSERT INTO public.ws_lib_tracks_to_be_synced(
				type, ref_id, status, date_created,created_by)
				VALUES ('l', OLD.library_id, 'created', CURRENT_TIMESTAMP, NEW.last_edited_by);
			ELSE
				UPDATE public.ws_lib_tracks_to_be_synced SET date_created=CURRENT_TIMESTAMP 
				WHERE ref_id=OLD.library_id;
			END IF;	
		ELSEIF(NEW.ml_status = 'ARCH') THEN
			IF (SELECT count(*) FROM public.ws_lib_tracks_to_be_synced WHERE ref_id=OLD.library_id) = 0 THEN
		 		INSERT INTO public.ws_lib_tracks_to_be_synced(
				type, ref_id, status, date_created,created_by)
				VALUES ('l', OLD.library_id, 'created', CURRENT_TIMESTAMP, NEW.last_edited_by);
			ELSE
				UPDATE public.ws_lib_tracks_to_be_synced SET date_created=CURRENT_TIMESTAMP 
				WHERE ref_id=OLD.library_id;
			END IF;	
		END IF;
		
	END IF;
	
	IF OLD.sync_status <> NEW.sync_status THEN
		_status_name := 'sync';
		_old_val := OLD.sync_status;
		_new_val := NEW.sync_status;
		
		INSERT INTO log.log_ws_lib_status_change(record_type,record_id,old_status,new_status,date_created,created_by,status_name)
    	VALUES ('l',OLD.library_id,_old_val,_new_val,CURRENT_TIMESTAMP,NEW.last_edited_by,_status_name); 
	END IF;	
	
	RETURN NEW;
END;
$$;


ALTER FUNCTION public.fn_lib_change_trigger() OWNER TO admin;

--
-- Name: fn_sync_track(); Type: FUNCTION; Schema: public; Owner: admin
--

CREATE FUNCTION public.fn_sync_track(OUT status integer) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE _LoopSyncSesson record;
DECLARE _LoopTrackLog record;

BEGIN
	status := 0;
	-- Check not synced sessions
	FOR _LoopSyncSesson in (select * from log.log_track_sync_session ltss where session_end is null) loop
		
		IF _LoopSyncSesson.total_synced_tracks > 0 THEN		
			-- Update tracks
			FOR _LoopTrackLog in (select * from log.log_track_api_results ltar 
				left join public.ml_master_track t2 on ltar.track_id = t2.track_id 
				where ltar.session_id = _LoopSyncSesson.session_id and t2.track_id is not null and ltar.version_id <> t2.dh_version_id) loop
				
					update public.ml_master_track set deleted = _LoopTrackLog.deleted,metadata = _LoopTrackLog.metadata,dh_version_id =_LoopTrackLog.version_id,
					date_last_edited = CURRENT_TIMESTAMP, received =_LoopTrackLog.received
					where track_id = _LoopTrackLog.track_id;
					
					update public.track_org set ml_status = 'SYNC', date_last_edited = CURRENT_TIMESTAMP, last_edited_by =_LoopTrackLog.created_by,
					deleted=_LoopTrackLog.deleted
					where id = _LoopTrackLog.track_id;
					
			END loop;
			
			-- Insert tracks
			INSERT INTO public.track_org(
			id, original_track_id, download_restricted, ml_status,album_id, date_created, created_by,deleted)
			select ltar.track_id,ltar.track_id,false,'SYNC',(ltar.metadata -> 'trackData'->'product'->>'id')::uuid, CURRENT_TIMESTAMP,ltar.created_by,ltar.deleted 
			from log.log_track_api_results ltar 
			left join public.ml_master_track t2 on ltar.track_id = t2.track_id 
			where ltar.session_id = _LoopSyncSesson.session_id and ltar.deleted = false 
			and t2.track_id is null;			
			
			insert into public.ml_master_track (track_id, workspace_id, dh_version_id, received, deleted, metadata, date_last_edited,restricted,library_id,
												album_id,ext_sys_ref,source_ref)
			select ltar.track_id,ltar.workspace_id,ltar.version_id,ltar.received,ltar.deleted,ltar.metadata,CURRENT_TIMESTAMP,false,
			(ltar.metadata -> 'trackData' ->> 'libraryId')::uuid,(ltar.metadata -> 'trackData'->'product'->>'id')::uuid,
			ltar.metadata -> 'trackData'->'identifiers'->>'extsysref',ltar.metadata -> 'trackData'->'miscellaneous'->>'sourceRef'
			from log.log_track_api_results ltar 
			left join public.ml_master_track t2 on ltar.track_id = t2.track_id 
			where ltar.session_id = _LoopSyncSesson.session_id and ltar.deleted = false 
			and t2.track_id is null;
			
		END IF;
		
		-- Update log_track_sync_session
		update log.log_track_sync_session set session_end = CURRENT_TIMESTAMP
		where session_id = _LoopSyncSesson.session_id;
			
	END loop;	
	status := 1;
RETURN;
END;
$$;


ALTER FUNCTION public.fn_sync_track(OUT status integer) OWNER TO admin;

--
-- Name: fn_trigger_sync_album(); Type: FUNCTION; Schema: public; Owner: admin
--

CREATE FUNCTION public.fn_trigger_sync_album() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE _master_row record;
BEGIN	
		IF NEW.deleted = true THEN
			update ml_master_album SET
			date_last_edited=CURRENT_TIMESTAMP,metadata=NEW.metadata,		
			archived=NEW.deleted where album_id = NEW.album_id;
			
			update public.track_org set date_last_edited=CURRENT_TIMESTAMP,source_deleted=NEW.deleted 
			where album_id=NEW.album_id;
			
			update public.album_org set date_last_edited=CURRENT_TIMESTAMP,source_deleted=NEW.deleted  
			where original_album_id=NEW.album_id;
		ELSE
		
			IF EXISTS (SELECT 1 FROM public.ml_master_album mt
			WHERE mt.album_id = NEW.album_id) THEN
			
				SELECT into _master_row * FROM public.ml_master_album mt
				WHERE mt.album_id = NEW.album_id;
				
			    IF _master_row.ml_version_id is null or _master_row.ml_version_id != NEW.version_id THEN	
			    -- No need to insert records when album download since album record is inserted when track download
					UPDATE public.ml_master_album SET
  					date_last_edited=CURRENT_TIMESTAMP,metadata=NEW.metadata,synced=true,ml_version_id=NEW.version_id,		
					archived=NEW.deleted where album_id=NEW.album_id;
				
					update public.track_org set date_last_edited=CURRENT_TIMESTAMP 
					where album_id=NEW.album_id;
				
					update public.album_org set date_last_edited=CURRENT_TIMESTAMP,source_deleted=NEW.deleted  
					where original_album_id=NEW.album_id;
				
				END IF;		
			ELSE
				INSERT INTO public.ml_master_album(
				album_id, metadata, workspace_id, archived, restricted, date_created, date_last_edited,api_result_id)
				VALUES (NEW.album_id,NEW.metadata, NEW.workspace_id, NEW.deleted,
				false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP,(select max(api_result_id)+1 from public.ml_master_album));	
			END IF;		
			
		END IF;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.fn_trigger_sync_album() OWNER TO admin;

--
-- Name: fn_trigger_sync_track(); Type: FUNCTION; Schema: public; Owner: admin
--

CREATE FUNCTION public.fn_trigger_sync_track() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE _master_row record;
BEGIN	
		SELECT into _master_row * FROM public.ml_master_track mt
		WHERE mt.track_id = NEW.track_id;		
		
		IF NOT FOUND and NEW.deleted = false THEN 		
			INSERT INTO public.ml_master_track (track_id, workspace_id, dh_version_id, received, deleted, metadata, date_last_edited,restricted,library_id,
			album_id,ext_sys_ref,source_ref,api_result_id,synced,dh_received)VALUES
			(NEW.track_id,NEW.workspace_id,NEW.version_id,NEW.received,NEW.deleted,NEW.metadata,CURRENT_TIMESTAMP,false,
			(NEW.metadata -> 'trackData' ->> 'libraryId')::uuid,(NEW.metadata -> 'trackData'->'product'->>'id')::uuid,
			NEW.metadata -> 'trackData'->'identifiers'->>'extsysref',NEW.metadata -> 'trackData'->'miscellaneous'->>'sourceRef',NEW.id,
			CASE
            WHEN NEW.api_call_id = 0 THEN false
            ELSE true
        	END,NEW.received);
		ELSE
			IF NEW.deleted = true THEN 		
				UPDATE public.ml_master_track
				set deleted = NEW.deleted,dh_version_id =NEW.version_id,
				date_last_edited = CURRENT_TIMESTAMP, received =NEW.received,
				album_id=(NEW.metadata -> 'trackData'->'product'->>'id')::uuid,
				api_result_id=NEW.id,synced=true				
				WHERE track_id=NEW.track_id;
			ELSE
				-- Check updated datetime before update
				IF NEW.api_call_id = 0 THEN
				    UPDATE public.ml_master_track
					set deleted = NEW.deleted,metadata = NEW.metadata,dh_version_id =NEW.version_id,
					date_last_edited = CURRENT_TIMESTAMP, received =NEW.received,
					api_result_id=NEW.id,
					album_id=(NEW.metadata -> 'trackData'->'product'->>'id')::uuid,synced=false,
					ml_version_id=NEW.version_id,dh_received=NEW.received,library_id=(NEW.metadata -> 'trackData' ->> 'libraryId')::uuid
					WHERE track_id=NEW.track_id;
				ELSIF _master_row.dh_received = 0 OR NEW.received > _master_row.received THEN
					UPDATE public.ml_master_track
					set deleted = NEW.deleted,metadata = NEW.metadata,dh_version_id =NEW.version_id,
					date_last_edited = CURRENT_TIMESTAMP, received =NEW.received,
					api_result_id=NEW.id,
					album_id=(NEW.metadata -> 'trackData'->'product'->>'id')::uuid,
					dh_received=NEW.received,
					library_id=(NEW.metadata -> 'trackData' ->> 'libraryId')::uuid,
					synced = CASE
            		WHEN NEW.api_call_id = 0 THEN false
            		ELSE true
        			END					
					WHERE track_id=NEW.track_id;
				END IF;
			END IF; 
		END IF; 
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.fn_trigger_sync_track() OWNER TO admin;

--
-- Name: fn_update_album(); Type: FUNCTION; Schema: public; Owner: admin
--

CREATE FUNCTION public.fn_update_album() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
	IF (NEW.metadata->'trackData'->'product'->>'id')::text <> '' THEN
	
		INSERT INTO public.ml_master_album(
		album_id, metadata, workspace_id, library_id, archived, restricted, date_created, date_last_edited,api_result_id,synced,ml_version_id)
		VALUES ((NEW.metadata->'trackData'->'product'->>'id')::uuid,(NEW.metadata->'trackData'->>'product')::json, NEW.workspace_id, NEW.library_id,
				false, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP,NEW.api_result_id,true,(NEW.metadata->'trackData'->'product'->>'versionId')::uuid)
		--ON CONFLICT (album_id) DO NOTHING; 
		ON CONFLICT (album_id) DO UPDATE 
  		SET metadata = (NEW.metadata->'trackData'->>'product')::json,date_last_edited=CURRENT_TIMESTAMP,
		library_id=NEW.library_id,workspace_id=NEW.workspace_id,api_result_id=NEW.api_result_id,
		archived=false,synced=true,ml_version_id=(NEW.metadata->'trackData'->'product'->>'versionId')::uuid;
		
	END IF; 
	
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.fn_update_album() OWNER TO admin;

--
-- Name: fn_update_elastic_album_change(); Type: FUNCTION; Schema: public; Owner: admin
--

CREATE FUNCTION public.fn_update_elastic_album_change() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
	IF NEW.ml_status = 2 THEN
		INSERT INTO log.elastic_album_change(
		document_id, original_album_id, album_org_data, date_created, deleted, archived, org_id, org_workspace_id,api_result_id)
		VALUES (NEW.id, NEW.original_album_id, row_to_json(NEW), CURRENT_TIMESTAMP,NEW.manually_deleted,false,NEW.org_id,New.org_workspace_id,NEW.api_result_id)
		ON CONFLICT (document_id) DO UPDATE set album_org_data=row_to_json(NEW),deleted=NEW.manually_deleted,api_result_id=NEW.api_result_id;
	ELSIF NEW.ml_status = 3 THEN
		INSERT INTO log.elastic_album_change(
		document_id, original_album_id, album_org_data, date_created, deleted, archived, org_id, org_workspace_id,api_result_id)
		VALUES (NEW.id, NEW.original_album_id, row_to_json(NEW), CURRENT_TIMESTAMP,NEW.manually_deleted,true,NEW.org_id,New.org_workspace_id,NEW.api_result_id)
		ON CONFLICT (document_id) DO UPDATE set album_org_data=row_to_json(NEW),deleted=NEW.manually_deleted,archived=true,api_result_id=NEW.api_result_id;
	ELSIF NEW.ml_status = 4 THEN
		INSERT INTO log.elastic_album_change(
		document_id, original_album_id, album_org_data, date_created, deleted, restricted, org_id, org_workspace_id,api_result_id)
		VALUES (NEW.id, NEW.original_album_id, row_to_json(NEW), CURRENT_TIMESTAMP,NEW.manually_deleted,true,NEW.org_id,New.org_workspace_id,NEW.api_result_id)
		ON CONFLICT (document_id) DO UPDATE set album_org_data=row_to_json(NEW),deleted=NEW.manually_deleted,restricted=true,api_result_id=NEW.api_result_id;
	END IF; 
	
	IF NEW.archive = true THEN	
		INSERT INTO log.elastic_album_change(
		document_id, original_album_id, album_org_data, date_created, deleted, archived, org_id, org_workspace_id,api_result_id)
		VALUES (NEW.id, NEW.original_album_id, row_to_json(NEW), CURRENT_TIMESTAMP,NEW.manually_deleted,true,NEW.org_id,New.org_workspace_id,NEW.api_result_id)
		ON CONFLICT (document_id) DO UPDATE set album_org_data=row_to_json(NEW),deleted=NEW.manually_deleted,archived=true,api_result_id=NEW.api_result_id;
	END IF; 
	
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.fn_update_elastic_album_change() OWNER TO admin;

--
-- Name: fn_update_elastic_track_change(); Type: FUNCTION; Schema: public; Owner: admin
--

CREATE FUNCTION public.fn_update_elastic_track_change() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
	IF NEW.ml_status = 2 THEN
		INSERT INTO log.elastic_track_change(
		document_id, original_track_id, track_org_data, date_created,album_id,deleted,org_id,org_workspace_id)
		VALUES (NEW.id, NEW.original_track_id, row_to_json(NEW), CURRENT_TIMESTAMP,NEW.album_id,NEW.source_deleted,NEW.org_id,New.org_workspace_id);
	ELSIF NEW.ml_status = 3 THEN
		INSERT INTO log.elastic_track_change(
		document_id, original_track_id, track_org_data, date_created,album_id,deleted,archived,org_id,org_workspace_id)
		VALUES (NEW.id, NEW.original_track_id, row_to_json(NEW), CURRENT_TIMESTAMP,NEW.album_id,NEW.source_deleted,true,NEW.org_id,New.org_workspace_id);
	ELSIF NEW.ml_status = 4 THEN
		INSERT INTO log.elastic_track_change(
		document_id, original_track_id, track_org_data, date_created,album_id,deleted,restricted,org_id,org_workspace_id)
		VALUES (NEW.id, NEW.original_track_id, row_to_json(NEW), CURRENT_TIMESTAMP,NEW.album_id,NEW.source_deleted,true,NEW.org_id,New.org_workspace_id);		
	END IF; 
	
	IF NEW.archive = true THEN	
		INSERT INTO log.elastic_track_change(
		document_id, original_track_id, track_org_data, date_created,album_id,deleted,archived,org_id,org_workspace_id)
		VALUES (NEW.id, NEW.original_track_id, row_to_json(NEW), CURRENT_TIMESTAMP,NEW.album_id,NEW.source_deleted,true,NEW.org_id,New.org_workspace_id);	
	END IF; 
	
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.fn_update_elastic_track_change() OWNER TO admin;

--
-- Name: fn_update_tag(uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: admin
--

CREATE FUNCTION public.fn_update_tag(_tag_type_id uuid, _track_id uuid, _tag_list character varying) RETURNS TABLE(track_id uuid, tag uuid, tag_with_type character varying)
    LANGUAGE plpgsql
    AS $$

DECLARE _tagVal varchar;
DECLARE _tagId uuid;

BEGIN

	delete from public.tag_track tt where tt.track_id = _track_id::uuid and tt.tag_with_type like concat('%',_tag_type_id::text,'%');
	
	foreach _tagVal in array string_to_array(_tag_list, ',')
	loop		
		IF NOT EXISTS (select * from public.tag t where t.tag_type_id=_tag_type_id and t.tag_value=_tagVal)  THEN
			_tagId := (select md5(random()::text || clock_timestamp()::text)::uuid);			
			INSERT INTO public.tag(
			tag_id, tag_type_id, tag_value, date_created, rating)
			VALUES (_tagId, _tag_type_id, _tagVal, CURRENT_TIMESTAMP, 100);
			
		ELSE
			_tagId := (select tag_id from public.tag t where t.tag_type_id=_tag_type_id and t.tag_value=_tagVal);				
		END IF;
		--raise notice 'Value: %', _tagId;
		
		INSERT INTO public.tag_track( track_id, tag, tag_with_type)
		VALUES (_track_id::uuid, _tagId, concat(_tag_type_id::text,':',_tagId::text));		
		
	end loop;    

	return query select tt.track_id ,tt.tag ,tt.tag_with_type from public.tag_track tt where tt.track_id = _track_id::uuid;

END;
$$;


ALTER FUNCTION public.fn_update_tag(_tag_type_id uuid, _track_id uuid, _tag_list character varying) OWNER TO admin;

--
-- Name: fn_ws_change_trigger(); Type: FUNCTION; Schema: public; Owner: admin
--

CREATE FUNCTION public.fn_ws_change_trigger() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE _status_name text DEFAULT null;
	DECLARE _old_val text DEFAULT null;
	DECLARE _new_val text DEFAULT null;

BEGIN	
	
	IF OLD.dh_status <> NEW.dh_status THEN
		_status_name := 'dh';
		_old_val := OLD.dh_status;
		_new_val := NEW.dh_status;
		
		INSERT INTO log.log_ws_lib_status_change(record_type,record_id,old_status,new_status,date_created,created_by,status_name)
    	VALUES ('w',OLD.workspace_id,_old_val,_new_val,CURRENT_TIMESTAMP,NEW.last_edited_by,_status_name); 
	END IF;
	
	IF OLD.ml_status <> NEW.ml_status THEN
		_status_name := 'ml';
		_old_val := OLD.ml_status;
		_new_val := NEW.ml_status;
		
		INSERT INTO log.log_ws_lib_status_change(record_type,record_id,old_status,new_status,date_created,created_by,status_name)
    	VALUES ('w',OLD.workspace_id,_old_val,_new_val,CURRENT_TIMESTAMP,NEW.last_edited_by,_status_name); 
		
		IF (NEW.ml_status = 'LIVE' or NEW.ml_status = 'ALIVE') THEN
		    -- Delete child libraries if WS status is LIVE or ALIVE
			DELETE FROM public.ws_lib_tracks_to_be_synced
			where "type" = 'l' and ref_id in (
				select l.library_id from "library" l 
				where l.ml_status <> 'ARCH' and l.workspace_id = OLD.workspace_id
			); 
			
			IF (SELECT count(*) FROM public.ws_lib_tracks_to_be_synced WHERE ref_id=OLD.workspace_id) = 0 THEN
		 		INSERT INTO public.ws_lib_tracks_to_be_synced(
				type, ref_id, status, date_created, created_by)
				VALUES ('w', OLD.workspace_id, 'created', CURRENT_TIMESTAMP, OLD.created_by);
			ELSE
				UPDATE public.ws_lib_tracks_to_be_synced SET date_created=CURRENT_TIMESTAMP 
				WHERE ref_id=OLD.workspace_id;
			END IF;
			
		ELSEIF(NEW.ml_status = 'ARCH') THEN	
		
			DELETE FROM public.ws_lib_tracks_to_be_synced
			where "type" = 'l' and ref_id in (
				select l.library_id from "library" l 
				where l.ml_status = 'ARCH' and l.workspace_id = OLD.workspace_id
			); 
			
		END IF;
		
	END IF;
	
	IF OLD.sync_status <> NEW.sync_status THEN
		_status_name := 'sync';
		_old_val := OLD.sync_status;
		_new_val := NEW.sync_status;
		
		INSERT INTO log.log_ws_lib_status_change(record_type,record_id,old_status,new_status,date_created,created_by,status_name)
    	VALUES ('w',OLD.workspace_id,_old_val,_new_val,CURRENT_TIMESTAMP,NEW.last_edited_by,_status_name); 
	END IF;	
	
	RETURN NEW;
END;
$$;


ALTER FUNCTION public.fn_ws_change_trigger() OWNER TO admin;

--
-- Name: sp_sync_library(integer); Type: PROCEDURE; Schema: public; Owner: admin
--

CREATE PROCEDURE public.sp_sync_library(p_user_id integer)
    LANGUAGE plpgsql
    AS $$
--DECLARE _newWS record;
DECLARE r_sw staging.staging_library%rowtype;
DECLARE r_w library%rowtype;
DECLARE _old_lb_neme record;

DECLARE _NewRecord record;
DECLARE _OldRecord record;
DECLARE _LoopRow record;
DECLARE _ActionType text DEFAULT '';

BEGIN
	-- Check new libraries
	FOR _LoopRow in (select  sw.* from staging.staging_library sw 
	WHERE (sw.library_id) not in (SELECT l.library_id FROM public.library l)) loop
		INSERT INTO public.library(library_id,library_name,workspace_id,track_count,archived,date_created,date_last_edited,dh_status,download_status,created_by) 
		VALUES (_LoopRow.library_id,_LoopRow.library_name,_LoopRow.workspace_id,_LoopRow.track_count,_LoopRow.deleted,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1,1,p_user_id);	
		INSERT INTO log.log_library_change(action_type,library_id,new_value,date_logged)
		VALUES ('NEW',_LoopRow.library_id,row_to_json(_LoopRow),CURRENT_TIMESTAMP);
	END loop;

	-- Check deleted libraries
	_OldRecord := null;
	_NewRecord := null;
	_LoopRow := null;
	FOR _LoopRow in (select l.* FROM library l  
	WHERE l.archived = false AND l.library_id not in (SELECT sl.library_id FROM staging.staging_library sl)) loop
	
		select * into _OldRecord from library where library_id = _LoopRow.library_id;
		
		UPDATE public.library set archived = true,date_last_edited = CURRENT_TIMESTAMP 
		WHERE library_id = _LoopRow.library_id;
		
		select * into _NewRecord from library where library_id = _LoopRow.library_id;
		
		INSERT INTO log.log_library_change(action_type,library_id,old_value,new_value,date_logged)
		VALUES ('RECDEL',_LoopRow.library_id,row_to_json(_OldRecord),row_to_json(_NewRecord),CURRENT_TIMESTAMP);
		
	END loop;
	
	-- Check name changes
	_OldRecord := null;
	_NewRecord := null;
	_LoopRow := null;
	FOR _LoopRow in (select sl.library_id,sl.library_name from staging.staging_library sl 
	where sl.library_id in (select l.library_id from public.library l)
	except
	select l.library_id,l.library_name from public.library l) loop	
	
		select * into _OldRecord from library where library_id = _LoopRow.library_id;
		
		UPDATE public.library set library_name = _LoopRow.library_name,date_last_edited = CURRENT_TIMESTAMP
		WHERE library_id = _LoopRow.library_id;	
	
		select * into _NewRecord from library where library_id = _LoopRow.library_id;
		
		select * into _old_lb_neme from public.library where library_id = _LoopRow.library_id;
		INSERT INTO log.log_library_change(action_type,library_id,old_value,new_value,date_logged)
		VALUES ('NAMECH',_LoopRow.library_id,row_to_json(_OldRecord),row_to_json(_NewRecord),CURRENT_TIMESTAMP);
			
	END loop;
	
	-- Check track count changes
	_OldRecord := null;
	_NewRecord := null;
	_LoopRow := null;
	FOR _LoopRow in (select sl.library_id,sl.track_count from staging.staging_library sl 
	where sl.library_id in (select l.library_id from public.library l)
	except
	select l.library_id,l.track_count from public.library l) loop	
		select * into _OldRecord from library where library_id = _LoopRow.library_id;
		
		UPDATE public.library set track_count = _LoopRow.track_count,date_last_edited = CURRENT_TIMESTAMP
		WHERE library_id = _LoopRow.library_id;	
		
		select * into _NewRecord from library where library_id = _LoopRow.library_id;
		
		INSERT INTO log.log_library_change(action_type,library_id,old_value,new_value,date_logged)
		VALUES ('TRCCH',_LoopRow.library_id,row_to_json(_OldRecord),row_to_json(_NewRecord),CURRENT_TIMESTAMP);
			
	END loop;
	
	-- Check deleted changes
	_OldRecord := null;
	_NewRecord := null;
	_LoopRow := null;
	FOR _LoopRow in (select sl.library_id,sl.deleted as deleted from staging.staging_library sl 
	where sl.library_id in (select l.library_id from public.library l)
	except
	select l.library_id,l.archived as deleted from public.library l) loop	
		select * into _OldRecord from library where library_id = _LoopRow.library_id;		
			
			IF _LoopRow.deleted = true THEN
				_ActionType = 'LOGDEL';
			ELSE
				_ActionType = 'REVOKE';
			END IF;
			
		UPDATE public.library set archived = _LoopRow.deleted,date_last_edited = CURRENT_TIMESTAMP
		WHERE library_id = _LoopRow.library_id;
		
		select * into _NewRecord from library where library_id = _LoopRow.library_id;
		
		INSERT INTO log.log_library_change(action_type,library_id,old_value,new_value,date_logged)
		VALUES (_ActionType,_LoopRow.library_id,row_to_json(_OldRecord),row_to_json(_NewRecord),CURRENT_TIMESTAMP);
		
	END loop;
	
	
END
$$;


ALTER PROCEDURE public.sp_sync_library(p_user_id integer) OWNER TO admin;

--
-- Name: sp_sync_workspace(integer); Type: PROCEDURE; Schema: public; Owner: admin
--

CREATE PROCEDURE public.sp_sync_workspace(p_user_id integer)
    LANGUAGE plpgsql
    AS $$
DECLARE _NewRecord record;
DECLARE _OldRecord record;
DECLARE _LoopRow record;
DECLARE _ActionType text DEFAULT '';

BEGIN
	-- Check new workspaces	
	FOR _LoopRow in (select  sw.* from staging.staging_workspace sw 
	WHERE (sw.workspace_id) not in (SELECT w.workspace_id FROM workspace w)) loop
		INSERT INTO public.workspace(workspace_id,workspace_name,archived,date_created,date_last_edited,dh_status,download_status,track_count,created_by) 
		VALUES (_LoopRow.workspace_id,_LoopRow.workspace_name,_LoopRow.deleted,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1,1,_LoopRow.track_count,p_user_id);	
		INSERT INTO log.log_workspace_change(action_type,workspace_id,new_value,date_logged)
		VALUES ('NEW',_LoopRow.workspace_id,row_to_json(_LoopRow),CURRENT_TIMESTAMP);
	END loop;

	-- Check deleted workspaces -- Generate notifications 
	_OldRecord := null;
	_NewRecord := null;
	_LoopRow := null;
	FOR _LoopRow in (select w.* FROM workspace w  
	WHERE w.archived = false AND w.workspace_id not in (SELECT ws.workspace_id FROM staging.staging_workspace ws)) loop	
	
		select * into _OldRecord from workspace where workspace_id = _LoopRow.workspace_id;
		
		UPDATE public.workspace set archived = true,date_last_edited = CURRENT_TIMESTAMP,last_edited_by=p_user_id 
		WHERE workspace_id = _LoopRow.workspace_id;
		
		select * into _NewRecord from workspace where workspace_id = _LoopRow.workspace_id;
		
		INSERT INTO log.log_workspace_change(action_type,workspace_id,old_value,new_value,date_logged)
		VALUES ('RECDEL',_LoopRow.workspace_id,row_to_json(_OldRecord),row_to_json(_NewRecord),CURRENT_TIMESTAMP);
	END loop;
	
	-- Check name changes	
	_OldRecord := null;
	_NewRecord := null;
	_LoopRow := null;
	FOR _LoopRow in (select sw.workspace_id,sw.workspace_name from staging.staging_workspace sw 
	where sw.workspace_id in (select w.workspace_id from workspace w)
	except
	select w.workspace_id,w.workspace_name from workspace w) loop	
		select * into _OldRecord from workspace where workspace_id = _LoopRow.workspace_id;
		
		UPDATE public.workspace set workspace_name = _LoopRow.workspace_name,date_last_edited = CURRENT_TIMESTAMP,last_edited_by=p_user_id 
		WHERE workspace_id = _LoopRow.workspace_id;	
		
		select * into _NewRecord from workspace where workspace_id = _LoopRow.workspace_id;
		
		INSERT INTO log.log_workspace_change(action_type,workspace_id,old_value,new_value,date_logged)
		VALUES ('NAMECH',_LoopRow.workspace_id,row_to_json(_OldRecord),row_to_json(_NewRecord),CURRENT_TIMESTAMP);
			
	END loop;
	
	-- Check track count changes
	_OldRecord := null;
	_NewRecord := null;
	_LoopRow := null;
	FOR _LoopRow in (select sw.workspace_id,sw.track_count from staging.staging_workspace sw 
	where sw.workspace_id in (select w.workspace_id from workspace w)
	except
	select w.workspace_id,w.track_count from workspace w) loop
	
		select * into _OldRecord from workspace where workspace_id = _LoopRow.workspace_id;
		
		UPDATE public.workspace set track_count = _LoopRow.track_count,date_last_edited = CURRENT_TIMESTAMP,last_edited_by=p_user_id
		WHERE workspace_id = _LoopRow.workspace_id;	
		
		select * into _NewRecord from workspace where workspace_id = _LoopRow.workspace_id;
		
		INSERT INTO log.log_workspace_change(action_type,workspace_id,old_value,new_value,date_logged)
		VALUES ('TRCCH',_LoopRow.workspace_id,row_to_json(_OldRecord),row_to_json(_NewRecord),CURRENT_TIMESTAMP);
			
	END loop;
	
	-- Check deleted changes
	_OldRecord := null;
	_NewRecord := null;
	_LoopRow := null;
	FOR _LoopRow in (select sw.workspace_id,sw.deleted as deleted from staging.staging_workspace sw 
	where sw.workspace_id in (select w.workspace_id from workspace w)
	except
	select w.workspace_id,w.archived as deleted from workspace w) loop	
		select * into _OldRecord from workspace where workspace_id = _LoopRow.workspace_id;		
			
			IF _LoopRow.deleted = true THEN
				_ActionType = 'LOGDEL';
			ELSE
				_ActionType = 'REVOKE';
			END IF;
			
		UPDATE public.workspace set archived = _LoopRow.deleted,date_last_edited = CURRENT_TIMESTAMP,last_edited_by=p_user_id
		WHERE workspace_id = _LoopRow.workspace_id;
		
		select * into _NewRecord from workspace where workspace_id = _LoopRow.workspace_id;
		
		INSERT INTO log.log_workspace_change(action_type,workspace_id,old_value,new_value,date_logged)
		VALUES (_ActionType,_LoopRow.workspace_id,row_to_json(_OldRecord),row_to_json(_NewRecord),CURRENT_TIMESTAMP);
		
	END loop;
	
END
$$;


ALTER PROCEDURE public.sp_sync_workspace(p_user_id integer) OWNER TO admin;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: chart_master_albums; Type: TABLE; Schema: charts; Owner: admin
--

CREATE TABLE charts.chart_master_albums (
    master_id uuid NOT NULL,
    title character varying,
    artist character varying,
    external_id character varying,
    first_date_released date,
    highest_date_released date,
    first_pos integer,
    highest_pos integer,
    chart_type_id uuid NOT NULL,
    chart_type_name character varying,
    date_created timestamp with time zone NOT NULL,
    date_last_edited timestamp with time zone NOT NULL
);


ALTER TABLE charts.chart_master_albums OWNER TO admin;

--
-- Name: chart_master_tracks; Type: TABLE; Schema: charts; Owner: admin
--

CREATE TABLE charts.chart_master_tracks (
    master_id uuid NOT NULL,
    title character varying,
    artist character varying,
    external_id character varying,
    first_date_released date,
    highest_date_released date,
    first_pos integer,
    highest_pos integer,
    chart_type_id uuid NOT NULL,
    chart_type_name character varying,
    date_created timestamp with time zone NOT NULL,
    date_last_edited timestamp with time zone NOT NULL
);


ALTER TABLE charts.chart_master_tracks OWNER TO admin;

--
-- Name: chart_sync_summary; Type: TABLE; Schema: charts; Owner: admin
--

CREATE TABLE charts.chart_sync_summary (
    id bigint NOT NULL,
    chart_type_id uuid,
    type "char" NOT NULL,
    check_date date NOT NULL,
    count integer,
    date_last_edited timestamp with time zone NOT NULL
);


ALTER TABLE charts.chart_sync_summary OWNER TO admin;

--
-- Name: chart_sync_summary_id_seq; Type: SEQUENCE; Schema: charts; Owner: admin
--

CREATE SEQUENCE charts.chart_sync_summary_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE charts.chart_sync_summary_id_seq OWNER TO admin;

--
-- Name: chart_sync_summary_id_seq; Type: SEQUENCE OWNED BY; Schema: charts; Owner: admin
--

ALTER SEQUENCE charts.chart_sync_summary_id_seq OWNED BY charts.chart_sync_summary.id;


--
-- Name: elastic_album_change; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.elastic_album_change (
    document_id uuid NOT NULL,
    original_album_id uuid NOT NULL,
    org_workspace_id uuid NOT NULL,
    org_id character varying NOT NULL,
    album_org_data json,
    deleted boolean,
    archived boolean,
    date_created timestamp without time zone NOT NULL,
    restricted boolean,
    api_result_id bigint
);


ALTER TABLE log.elastic_album_change OWNER TO admin;

--
-- Name: elastic_track_change; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.elastic_track_change (
    id bigint NOT NULL,
    document_id uuid NOT NULL,
    original_track_id uuid,
    dh_version_id uuid,
    track_org_data json NOT NULL,
    date_created timestamp without time zone NOT NULL,
    album_id uuid,
    deleted boolean,
    archived boolean DEFAULT false,
    org_id character varying,
    org_workspace_id uuid NOT NULL,
    restricted boolean
);


ALTER TABLE log.elastic_track_change OWNER TO admin;

--
-- Name: elastic_track_change_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.elastic_track_change_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.elastic_track_change_id_seq OWNER TO admin;

--
-- Name: elastic_track_change_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.elastic_track_change_id_seq OWNED BY log.elastic_track_change.id;


--
-- Name: log_album_api_calls; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_album_api_calls (
    id bigint NOT NULL,
    date_created timestamp without time zone NOT NULL,
    ws_id uuid,
    page_size integer NOT NULL,
    page_token character varying,
    library_ids character varying,
    response_code integer,
    next_page_token character varying,
    album_count integer NOT NULL,
    session_id integer
);


ALTER TABLE log.log_album_api_calls OWNER TO admin;

--
-- Name: log_album_api_calls_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_album_api_calls_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_album_api_calls_id_seq OWNER TO admin;

--
-- Name: log_album_api_calls_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_album_api_calls_id_seq OWNED BY log.log_album_api_calls.id;


--
-- Name: log_album_api_results; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_album_api_results (
    id bigint NOT NULL,
    api_call_id bigint NOT NULL,
    album_id uuid NOT NULL,
    workspace_id uuid,
    version_id uuid,
    received bigint,
    deleted boolean,
    metadata json,
    session_id integer,
    date_created date NOT NULL,
    created_by uuid NOT NULL,
    date_modified timestamp without time zone
);


ALTER TABLE log.log_album_api_results OWNER TO admin;

--
-- Name: log_album_api_results_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_album_api_results_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_album_api_results_id_seq OWNER TO admin;

--
-- Name: log_album_api_results_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_album_api_results_id_seq OWNED BY log.log_album_api_results.id;


--
-- Name: log_album_sync_session; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_album_sync_session (
    session_id bigint NOT NULL,
    session_start timestamp without time zone NOT NULL,
    session_end timestamp without time zone,
    workspace_id uuid NOT NULL,
    synced_tracks_count integer,
    download_time time without time zone,
    download_tracks_count integer,
    status boolean,
    page_token json
);


ALTER TABLE log.log_album_sync_session OWNER TO admin;

--
-- Name: log_album_sync_session_session_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_album_sync_session_session_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_album_sync_session_session_id_seq OWNER TO admin;

--
-- Name: log_album_sync_session_session_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_album_sync_session_session_id_seq OWNED BY log.log_album_sync_session.session_id;


--
-- Name: log_elastic_track_changes; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_elastic_track_changes (
    id bigint NOT NULL,
    track_id uuid NOT NULL,
    metadata json,
    date_created timestamp without time zone NOT NULL,
    processed boolean NOT NULL,
    version_id uuid NOT NULL,
    received bigint NOT NULL,
    deleted boolean NOT NULL,
    restricted boolean NOT NULL
);


ALTER TABLE log.log_elastic_track_changes OWNER TO admin;

--
-- Name: log_elastic_track_changes_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_elastic_track_changes_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_elastic_track_changes_id_seq OWNER TO admin;

--
-- Name: log_elastic_track_changes_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_elastic_track_changes_id_seq OWNED BY log.log_elastic_track_changes.id;


--
-- Name: log_library_change; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_library_change (
    libch_id bigint NOT NULL,
    action_type character varying(6) NOT NULL,
    library_id uuid,
    old_value json,
    new_value json,
    date_logged timestamp without time zone NOT NULL
);


ALTER TABLE log.log_library_change OWNER TO admin;

--
-- Name: log_library_change_libch_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_library_change_libch_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_library_change_libch_id_seq OWNER TO admin;

--
-- Name: log_library_change_libch_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_library_change_libch_id_seq OWNED BY log.log_library_change.libch_id;


--
-- Name: log_prs_search_time; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_prs_search_time (
    id bigint NOT NULL,
    track_id uuid NOT NULL,
    search_type character varying NOT NULL,
    search_query character varying,
    "time" time without time zone NOT NULL,
    date_created timestamp without time zone NOT NULL
);


ALTER TABLE log.log_prs_search_time OWNER TO admin;

--
-- Name: log_prs_search_time_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_prs_search_time_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_prs_search_time_id_seq OWNER TO admin;

--
-- Name: log_prs_search_time_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_prs_search_time_id_seq OWNED BY log.log_prs_search_time.id;


--
-- Name: log_sync_time; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_sync_time (
    id bigint NOT NULL,
    workspace_id uuid NOT NULL,
    org_id character varying,
    track_download_start_time timestamp without time zone,
    track_download_end_time timestamp without time zone,
    track_download_time time without time zone,
    album_download_start_time timestamp without time zone,
    album_download_end_time timestamp without time zone,
    album_download_time time without time zone,
    sync_start_time timestamp without time zone,
    sync_end_time timestamp without time zone,
    sync_time time without time zone,
    track_index_start_time timestamp without time zone,
    track_index_end_time timestamp without time zone,
    track_index_time time without time zone,
    album_index_start_time timestamp without time zone,
    album_index_end_time timestamp without time zone,
    album_index_time time without time zone,
    total_time time without time zone,
    status integer,
    download_tracks_count integer,
    download_albums_count integer,
    sync_tracks_count integer,
    sync_albums_count integer,
    index_tracks_count integer,
    index_albums_count integer,
    completed_time timestamp without time zone,
    service_id integer
);


ALTER TABLE log.log_sync_time OWNER TO admin;

--
-- Name: log_sync_time_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_sync_time_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_sync_time_id_seq OWNER TO admin;

--
-- Name: log_sync_time_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_sync_time_id_seq OWNED BY log.log_sync_time.id;


--
-- Name: log_track_api_calls; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_track_api_calls (
    id bigint NOT NULL,
    date_created timestamp without time zone NOT NULL,
    ws_id uuid,
    page_size integer NOT NULL,
    page_token character varying,
    library_ids character varying,
    response_code integer,
    next_page_token character varying,
    track_count integer NOT NULL,
    session_id integer
);


ALTER TABLE log.log_track_api_calls OWNER TO admin;

--
-- Name: log_track_api_calls_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_track_api_calls_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_track_api_calls_id_seq OWNER TO admin;

--
-- Name: log_track_api_calls_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_track_api_calls_id_seq OWNED BY log.log_track_api_calls.id;


--
-- Name: log_track_api_results_20240301; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_track_api_results_20240301 (
    id bigint NOT NULL,
    api_call_id bigint NOT NULL,
    track_id uuid NOT NULL,
    workspace_id uuid,
    version_id uuid,
    received bigint,
    deleted boolean,
    metadata json,
    session_id integer,
    date_created date NOT NULL,
    created_by uuid NOT NULL
);


ALTER TABLE log.log_track_api_results_20240301 OWNER TO admin;

--
-- Name: log_track_api_results_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_track_api_results_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_track_api_results_id_seq OWNER TO admin;

--
-- Name: log_track_api_results_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_track_api_results_id_seq OWNED BY log.log_track_api_results_20240301.id;


--
-- Name: log_track_api_results; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_track_api_results (
    id bigint DEFAULT nextval('log.log_track_api_results_id_seq'::regclass) NOT NULL,
    api_call_id bigint NOT NULL,
    track_id uuid NOT NULL,
    workspace_id uuid,
    version_id uuid,
    received bigint,
    deleted boolean,
    metadata json,
    session_id integer,
    date_created date NOT NULL,
    created_by uuid NOT NULL
);


ALTER TABLE log.log_track_api_results OWNER TO admin;

--
-- Name: log_track_index_error; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_track_index_error (
    id integer NOT NULL,
    doc_id uuid,
    error character varying,
    reson character varying
);


ALTER TABLE log.log_track_index_error OWNER TO admin;

--
-- Name: log_track_index_error_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_track_index_error_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_track_index_error_id_seq OWNER TO admin;

--
-- Name: log_track_index_error_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_track_index_error_id_seq OWNED BY log.log_track_index_error.id;


--
-- Name: log_track_sync_session; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_track_sync_session (
    session_id integer NOT NULL,
    session_start timestamp without time zone NOT NULL,
    session_end timestamp without time zone,
    workspace_id uuid NOT NULL,
    synced_tracks_count integer,
    download_time time without time zone,
    download_tracks_count integer,
    status boolean,
    page_token json
);


ALTER TABLE log.log_track_sync_session OWNER TO admin;

--
-- Name: log_track_sync_session_session_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_track_sync_session_session_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_track_sync_session_session_id_seq OWNER TO admin;

--
-- Name: log_track_sync_session_session_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_track_sync_session_session_id_seq OWNED BY log.log_track_sync_session.session_id;


--
-- Name: log_user_action; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_user_action (
    id bigint NOT NULL,
    action_id integer NOT NULL,
    user_id integer,
    date_created timestamp without time zone NOT NULL,
    org_id character varying NOT NULL,
    old_value character varying,
    new_value character varying,
    data_type character varying(5) NOT NULL,
    ref_id uuid,
    data_value character varying,
    status integer NOT NULL,
    exception character varying
);


ALTER TABLE log.log_user_action OWNER TO admin;

--
-- Name: log_user_action_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_user_action_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_user_action_id_seq OWNER TO admin;

--
-- Name: log_user_action_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_user_action_id_seq OWNED BY log.log_user_action.id;


--
-- Name: log_workspace_change; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_workspace_change (
    wsch_id bigint NOT NULL,
    action_type character varying(6),
    workspace_id uuid NOT NULL,
    old_value json,
    new_value json,
    date_logged timestamp without time zone
);


ALTER TABLE log.log_workspace_change OWNER TO admin;

--
-- Name: log_workspace_change_wsch_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_workspace_change_wsch_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_workspace_change_wsch_id_seq OWNER TO admin;

--
-- Name: log_workspace_change_wsch_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_workspace_change_wsch_id_seq OWNED BY log.log_workspace_change.wsch_id;


--
-- Name: log_ws_lib_change; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_ws_lib_change (
    ws_lib_change_id bigint NOT NULL,
    record_type "char" NOT NULL,
    action "char" NOT NULL,
    key uuid NOT NULL,
    value json,
    date_loged date NOT NULL,
    time_loged timestamp without time zone NOT NULL
);


ALTER TABLE log.log_ws_lib_change OWNER TO admin;

--
-- Name: log_ws_lib_status_change; Type: TABLE; Schema: log; Owner: admin
--

CREATE TABLE log.log_ws_lib_status_change (
    id bigint NOT NULL,
    record_type "char" NOT NULL,
    record_id uuid NOT NULL,
    status_name character varying(6) NOT NULL,
    old_status character varying(5) NOT NULL,
    new_status character varying(5) NOT NULL,
    date_created timestamp without time zone NOT NULL,
    created_by integer
);


ALTER TABLE log.log_ws_lib_status_change OWNER TO admin;

--
-- Name: log_ws_lib_status_change_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.log_ws_lib_status_change_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.log_ws_lib_status_change_id_seq OWNER TO admin;

--
-- Name: log_ws_lib_status_change_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.log_ws_lib_status_change_id_seq OWNED BY log.log_ws_lib_status_change.id;


--
-- Name: ws_lib_change_ws_lib_change_id_seq; Type: SEQUENCE; Schema: log; Owner: admin
--

CREATE SEQUENCE log.ws_lib_change_ws_lib_change_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE log.ws_lib_change_ws_lib_change_id_seq OWNER TO admin;

--
-- Name: ws_lib_change_ws_lib_change_id_seq; Type: SEQUENCE OWNED BY; Schema: log; Owner: admin
--

ALTER SEQUENCE log.ws_lib_change_ws_lib_change_id_seq OWNED BY log.log_ws_lib_change.ws_lib_change_id;


--
-- Name: playout_response; Type: TABLE; Schema: playout; Owner: admin
--

CREATE TABLE playout.playout_response (
    response_id bigint NOT NULL,
    build_id uuid NOT NULL,
    response_json json,
    status character varying,
    response_time bigint,
    request_id uuid
);


ALTER TABLE playout.playout_response OWNER TO admin;

--
-- Name: playout_response_response_id_seq; Type: SEQUENCE; Schema: playout; Owner: admin
--

CREATE SEQUENCE playout.playout_response_response_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE playout.playout_response_response_id_seq OWNER TO admin;

--
-- Name: playout_response_response_id_seq; Type: SEQUENCE OWNED BY; Schema: playout; Owner: admin
--

ALTER SEQUENCE playout.playout_response_response_id_seq OWNED BY playout.playout_response.response_id;


--
-- Name: playout_response_status; Type: TABLE; Schema: playout; Owner: admin
--

CREATE TABLE playout.playout_response_status (
    id bigint NOT NULL,
    status character varying,
    display_status character varying
);


ALTER TABLE playout.playout_response_status OWNER TO admin;

--
-- Name: playout_response_status_id_seq; Type: SEQUENCE; Schema: playout; Owner: admin
--

CREATE SEQUENCE playout.playout_response_status_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE playout.playout_response_status_id_seq OWNER TO admin;

--
-- Name: playout_response_status_id_seq; Type: SEQUENCE OWNED BY; Schema: playout; Owner: admin
--

ALTER SEQUENCE playout.playout_response_status_id_seq OWNED BY playout.playout_response_status.id;


--
-- Name: playout_session; Type: TABLE; Schema: playout; Owner: admin
--

CREATE TABLE playout.playout_session (
    id integer NOT NULL,
    org_id character varying NOT NULL,
    session_date date NOT NULL,
    track_count integer,
    last_status integer,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone NOT NULL,
    created_by integer NOT NULL,
    station_id uuid,
    build_id uuid,
    request_json json,
    last_edited_by integer,
    signiant_ref_id character varying,
    publish_status integer,
    publish_attempts integer,
    s3_cleanup boolean DEFAULT false NOT NULL,
    publish_start_datetime timestamp without time zone,
    notification_sent boolean DEFAULT false NOT NULL
);


ALTER TABLE playout.playout_session OWNER TO admin;

--
-- Name: playout_session_id_seq; Type: SEQUENCE; Schema: playout; Owner: admin
--

CREATE SEQUENCE playout.playout_session_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE playout.playout_session_id_seq OWNER TO admin;

--
-- Name: playout_session_id_seq; Type: SEQUENCE OWNED BY; Schema: playout; Owner: admin
--

ALTER SEQUENCE playout.playout_session_id_seq OWNED BY playout.playout_session.id;


--
-- Name: radio_stations; Type: TABLE; Schema: playout; Owner: admin
--

CREATE TABLE playout.radio_stations (
    id uuid NOT NULL,
    sys character varying NOT NULL,
    station character varying NOT NULL,
    delivery_location character varying NOT NULL,
    created_by integer NOT NULL,
    date_created timestamp without time zone NOT NULL,
    "order" integer,
    category_id integer,
    delivery_location_classical character varying
);


ALTER TABLE playout.radio_stations OWNER TO admin;

--
-- Name: org_user; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.org_user (
    user_id integer NOT NULL,
    org_id character varying NOT NULL,
    first_name character varying NOT NULL,
    last_name character varying,
    email character varying,
    role_id integer,
    date_last_edited timestamp without time zone,
    image_url character varying
);


ALTER TABLE public.org_user OWNER TO admin;

--
-- Name: playout_session_search; Type: VIEW; Schema: playout; Owner: admin
--

CREATE VIEW playout.playout_session_search AS
 SELECT ps.id,
    ps.org_id,
    ps.session_date,
    ps.track_count,
    ps.last_status,
    ps.station_id,
    ps.created_by,
    ps.date_created,
    ps.date_last_edited,
    concat(c_ou.first_name, ' ', c_ou.last_name) AS created_user,
    c_ou.image_url AS created_user_img,
    rs.station AS station_name
   FROM ((playout.playout_session ps
     LEFT JOIN public.org_user c_ou ON ((ps.created_by = c_ou.user_id)))
     LEFT JOIN playout.radio_stations rs ON ((ps.station_id = rs.id)));


ALTER TABLE playout.playout_session_search OWNER TO admin;

--
-- Name: playout_session_tracks; Type: TABLE; Schema: playout; Owner: admin
--

CREATE TABLE playout.playout_session_tracks (
    id bigint NOT NULL,
    session_id integer NOT NULL,
    track_id uuid NOT NULL,
    type integer NOT NULL,
    status integer NOT NULL,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone NOT NULL,
    created_by integer NOT NULL,
    last_edited_by integer NOT NULL,
    title character varying NOT NULL,
    isrc character varying NOT NULL,
    performer character varying NOT NULL,
    track_type character varying NOT NULL,
    artwork_url character varying,
    album_title character varying,
    label character varying,
    dh_track_id character varying,
    duration real,
    xml_status integer,
    asset_status integer
);


ALTER TABLE playout.playout_session_tracks OWNER TO admin;

--
-- Name: playout_session_tracks_id_seq; Type: SEQUENCE; Schema: playout; Owner: admin
--

CREATE SEQUENCE playout.playout_session_tracks_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE playout.playout_session_tracks_id_seq OWNER TO admin;

--
-- Name: playout_session_tracks_id_seq; Type: SEQUENCE OWNED BY; Schema: playout; Owner: admin
--

ALTER SEQUENCE playout.playout_session_tracks_id_seq OWNED BY playout.playout_session_tracks.id;


--
-- Name: radio_categories; Type: TABLE; Schema: playout; Owner: admin
--

CREATE TABLE playout.radio_categories (
    category_id integer NOT NULL,
    category_name character varying NOT NULL
);


ALTER TABLE playout.radio_categories OWNER TO admin;

--
-- Name: album_org; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.album_org (
    id uuid NOT NULL,
    original_album_id uuid NOT NULL,
    org_id character varying NOT NULL,
    change_log json,
    tags json,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone,
    c_tags json,
    source_deleted boolean,
    restricted boolean,
    archive boolean,
    org_data json,
    created_by integer NOT NULL,
    last_edited_by integer NOT NULL,
    ml_status integer NOT NULL,
    manually_deleted boolean,
    org_workspace_id uuid NOT NULL,
    api_result_id bigint NOT NULL,
    chart_info json,
    chart_artist boolean,
    content_alert boolean,
    content_alerted_date timestamp without time zone,
    content_alerted_user integer,
    ca_resolved_date timestamp without time zone,
    ca_resolved_user integer,
    alert_type integer,
    alert_note character varying
);


ALTER TABLE public.album_org OWNER TO admin;

--
-- Name: c_tag; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.c_tag (
    id integer NOT NULL,
    type character varying NOT NULL,
    name character varying NOT NULL,
    description character varying,
    date_created timestamp without time zone,
    condition json,
    created_by integer,
    colour character varying,
    date_last_edited timestamp without time zone,
    last_edited_by integer,
    is_restricted boolean,
    status integer,
    indicator character varying(6),
    display_indicator boolean,
    group_id integer,
    rule_group_index uuid,
    rule_group_index_updated_on timestamp without time zone
);


ALTER TABLE public.c_tag OWNER TO admin;

--
-- Name: c_tag_extended; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.c_tag_extended (
    id integer NOT NULL,
    c_tag_id integer NOT NULL,
    name character varying NOT NULL,
    description character varying,
    condition json,
    date_created timestamp without time zone,
    created_by integer,
    date_last_edited timestamp without time zone,
    last_edited_by integer,
    color character varying,
    status integer,
    track_id character varying,
    notes character varying
);


ALTER TABLE public.c_tag_extended OWNER TO admin;

--
-- Name: c_tag_extended_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.c_tag_extended_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.c_tag_extended_id_seq OWNER TO admin;

--
-- Name: c_tag_extended_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.c_tag_extended_id_seq OWNED BY public.c_tag_extended.id;


--
-- Name: c_tag_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.c_tag_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.c_tag_id_seq OWNER TO admin;

--
-- Name: c_tag_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.c_tag_id_seq OWNED BY public.c_tag.id;


--
-- Name: c_tag_index_status; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.c_tag_index_status (
    type character varying NOT NULL,
    updated_on timestamp with time zone NOT NULL,
    updated_by integer,
    update_idetifier uuid NOT NULL
);


ALTER TABLE public.c_tag_index_status OWNER TO admin;

--
-- Name: cleansed_tag_track; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.cleansed_tag_track (
    id bigint NOT NULL,
    track_id uuid NOT NULL,
    organization_id uuid,
    tag_type_id uuid,
    tag_type_name character varying NOT NULL,
    tag_name character varying NOT NULL,
    rating integer NOT NULL,
    is_cleansed boolean,
    cleansed_algorithm_id character varying,
    created_by uuid,
    date_created timestamp without time zone NOT NULL,
    last_edited_by uuid,
    date_last_edited timestamp without time zone
);


ALTER TABLE public.cleansed_tag_track OWNER TO admin;

--
-- Name: cleansed_tag_track_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.cleansed_tag_track_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.cleansed_tag_track_id_seq OWNER TO admin;

--
-- Name: cleansed_tag_track_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.cleansed_tag_track_id_seq OWNED BY public.cleansed_tag_track.id;


--
-- Name: ctag_extended_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.ctag_extended_search AS
 SELECT cte.id,
    cte.c_tag_id,
    cte.name,
    cte.description,
    cte.condition,
    cte.date_created,
    cte.created_by,
    cte.date_last_edited,
    cte.last_edited_by,
    cte.notes,
    cte.color,
    cte.status,
    ct.type,
    ct.name AS ctag_name,
    concat(c_ou.first_name, ' ', c_ou.last_name) AS created_user
   FROM ((public.c_tag_extended cte
     LEFT JOIN public.c_tag ct ON ((cte.c_tag_id = ct.id)))
     LEFT JOIN public.org_user c_ou ON ((cte.created_by = c_ou.user_id)));


ALTER TABLE public.ctag_extended_search OWNER TO admin;

--
-- Name: ctag_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.ctag_search AS
 SELECT ct.id,
    ct.name,
    ct.description,
    ct.condition,
    ct.date_created,
    ct.created_by,
    ct.date_last_edited,
    ct.last_edited_by,
    ct.colour,
    ct.type,
    ct.status,
    ct.indicator,
    ct.display_indicator,
    ct.group_id,
    concat(c_ou.first_name, ' ', c_ou.last_name) AS created_user
   FROM (public.c_tag ct
     LEFT JOIN public.org_user c_ou ON ((ct.created_by = c_ou.user_id)));


ALTER TABLE public.ctag_search OWNER TO admin;

--
-- Name: dh_status; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.dh_status (
    status_code character varying(5),
    status character varying
);


ALTER TABLE public.dh_status OWNER TO admin;

--
-- Name: library; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.library (
    library_id uuid NOT NULL,
    library_name character varying,
    workspace_id uuid NOT NULL,
    track_count integer NOT NULL,
    created_by integer,
    notes character varying,
    date_last_edited timestamp without time zone,
    last_edited_by integer,
    archived boolean,
    date_created timestamp without time zone,
    ml_track_count integer,
    dh_status integer NOT NULL,
    download_status integer NOT NULL,
    group_ids bigint[]
);


ALTER TABLE public.library OWNER TO admin;

--
-- Name: library_org; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.library_org (
    org_library_id uuid NOT NULL,
    library_id uuid NOT NULL,
    org_id character varying NOT NULL,
    ml_status integer NOT NULL,
    sync_status integer NOT NULL,
    restricted boolean NOT NULL,
    archived boolean NOT NULL,
    notes character varying,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone NOT NULL,
    created_by integer NOT NULL,
    last_edited_by integer NOT NULL,
    workspace_id uuid,
    album_sync_status integer,
    last_sync_api_result_id bigint NOT NULL,
    last_album_sync_api_result_id bigint NOT NULL,
    group_ids bigint[]
);


ALTER TABLE public.library_org OWNER TO admin;

--
-- Name: workspace; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.workspace (
    workspace_id uuid NOT NULL,
    workspace_name character varying NOT NULL,
    info json,
    restricted boolean,
    wslib_id uuid,
    notes character varying,
    archived boolean NOT NULL,
    created_by integer,
    last_edited_by integer,
    date_last_edited timestamp without time zone NOT NULL,
    date_created timestamp without time zone NOT NULL,
    track_count integer NOT NULL,
    next_page_token character varying,
    last_sync_date timestamp without time zone,
    ml_track_count integer,
    dh_status integer NOT NULL,
    download_status integer NOT NULL,
    album_next_page_token character varying,
    priority_sync integer DEFAULT 0,
    group_ids bigint[]
);


ALTER TABLE public.workspace OWNER TO admin;

--
-- Name: library_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.library_search AS
 SELECT (l.library_id)::text AS id,
    l.library_name,
    l.workspace_id,
    l.track_count,
    l.ml_track_count,
    l.created_by,
    l.dh_status,
    l.notes,
    l.download_status,
    l.date_last_edited,
    l.last_edited_by,
    l.archived,
    l.date_created,
    w.workspace_name,
    lo.ml_status,
    lo.group_ids
   FROM ((public.library l
     LEFT JOIN public.workspace w ON ((l.workspace_id = w.workspace_id)))
     LEFT JOIN public.library_org lo ON ((lo.library_id = l.library_id)))
  WHERE (l.archived = false);


ALTER TABLE public.library_search OWNER TO admin;

--
-- Name: library_search_ml_admin; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.library_search_ml_admin AS
 SELECT (l.library_id)::text AS id,
    lo.org_library_id AS olbid,
    l.library_name,
    l.workspace_id,
    l.track_count,
    l.ml_track_count,
    l.created_by,
    l.dh_status,
    l.notes,
    l.download_status,
    l.date_last_edited,
    l.last_edited_by,
    l.archived,
    l.date_created,
    w.workspace_name,
    lo.ml_status,
    lo.org_id,
    lo.group_ids
   FROM ((public.library l
     LEFT JOIN public.workspace w ON ((l.workspace_id = w.workspace_id)))
     LEFT JOIN public.library_org lo ON ((lo.library_id = l.library_id)))
  WHERE ((l.archived = false) AND (lo.ml_status <> 5));


ALTER TABLE public.library_search_ml_admin OWNER TO admin;

--
-- Name: member_label; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.member_label (
    id integer NOT NULL,
    member character varying,
    label character varying,
    mlc character varying,
    source character varying(6),
    date_created timestamp without time zone,
    date_last_edited timestamp without time zone,
    created_by integer,
    last_edited_by integer
);


ALTER TABLE public.member_label OWNER TO admin;

--
-- Name: member_label_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.member_label_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.member_label_id_seq OWNER TO admin;

--
-- Name: member_label_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.member_label_id_seq OWNED BY public.member_label.id;


--
-- Name: ml_master_album; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.ml_master_album (
    album_id uuid NOT NULL,
    metadata json,
    workspace_id uuid,
    library_id uuid,
    archived boolean,
    restricted boolean,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone NOT NULL,
    api_result_id bigint NOT NULL,
    synced boolean,
    ml_version_id uuid
);


ALTER TABLE public.ml_master_album OWNER TO admin;

--
-- Name: ml_master_track; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.ml_master_track (
    track_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    library_id uuid,
    dh_version_id uuid NOT NULL,
    received bigint NOT NULL,
    deleted boolean NOT NULL,
    metadata json,
    album_id uuid,
    date_last_edited timestamp without time zone NOT NULL,
    restricted boolean,
    dh_status character varying(5),
    external_identifiers json,
    source_ref character varying,
    ext_sys_ref character varying,
    edit_track_metadata json,
    edit_album_metadata json,
    pre_release boolean,
    api_result_id bigint NOT NULL,
    synced boolean,
    ml_version_id uuid,
    dh_received bigint
);


ALTER TABLE public.ml_master_track OWNER TO admin;

--
-- Name: ml_status; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.ml_status (
    status_code character varying(5),
    status character varying
);


ALTER TABLE public.ml_status OWNER TO admin;

--
-- Name: org_exclude; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.org_exclude (
    id integer NOT NULL,
    item_type character varying NOT NULL,
    ref_id uuid NOT NULL,
    organization character varying NOT NULL,
    date_created timestamp without time zone NOT NULL,
    created_by integer NOT NULL
);


ALTER TABLE public.org_exclude OWNER TO admin;

--
-- Name: org_exclude_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.org_exclude_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.org_exclude_id_seq OWNER TO admin;

--
-- Name: org_exclude_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.org_exclude_id_seq OWNED BY public.org_exclude.id;


--
-- Name: org_track_version; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.org_track_version (
    ml_version_id uuid NOT NULL,
    org_id uuid NOT NULL,
    original_track_id uuid NOT NULL,
    original_workspace_id uuid,
    new_workspace_id uuid,
    received bigint,
    deleted boolean NOT NULL,
    metadata json NOT NULL,
    "DateCreated" timestamp without time zone,
    "CreatedBy" uuid,
    "DateLastEdited" timestamp without time zone,
    "LastEditedBy" uuid
);


ALTER TABLE public.org_track_version OWNER TO admin;

--
-- Name: org_workspace; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.org_workspace (
    id integer NOT NULL,
    ws_type character varying,
    organization character varying,
    dh_ws_id uuid,
    date_created timestamp without time zone,
    created_by integer
);


ALTER TABLE public.org_workspace OWNER TO admin;

--
-- Name: org_workspace_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.org_workspace_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.org_workspace_id_seq OWNER TO admin;

--
-- Name: org_workspace_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.org_workspace_id_seq OWNED BY public.org_workspace.id;


--
-- Name: playout_session; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.playout_session (
    id integer NOT NULL,
    org_id character varying NOT NULL,
    session_date date NOT NULL,
    track_count integer,
    status integer,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone NOT NULL,
    created_by integer NOT NULL,
    station_id uuid
);


ALTER TABLE public.playout_session OWNER TO admin;

--
-- Name: playout_session_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.playout_session_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.playout_session_id_seq OWNER TO admin;

--
-- Name: playout_session_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.playout_session_id_seq OWNED BY public.playout_session.id;


--
-- Name: playout_session_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.playout_session_search AS
 SELECT table1.id,
    table1.org_id,
    table1.session_date,
    table1.station_id,
    table1.created_by,
    table1.date_created,
    table1.date_last_edited,
    table1.build_id,
    table1.last_status,
    table1.track_count,
    table1.publish_sucess_track_count,
    table1.created_user_img,
    table1.station_name,
    table1.created_user,
    table1.signiant_ref_id,
    table1.publish_status,
    table1.publish_start_datetime,
    table1.notification_sent,
    table1.publish_attempts,
        CASE
            WHEN ((table1.track_count = table1.publish_sucess_track_count) AND (table1.track_count > 0)) THEN true
            ELSE false
        END AS fully_publised
   FROM ( SELECT ps.id,
            ps.org_id,
            ps.session_date,
            ps.station_id,
            ps.created_by,
            ps.date_created,
            ps.date_last_edited,
            (ps.build_id)::text AS build_id,
            ps.last_status,
            ( SELECT count(1) AS count
                   FROM playout.playout_session_tracks pt
                  WHERE (pt.session_id = ps.id)) AS track_count,
            ( SELECT count(1) AS count
                   FROM playout.playout_session_tracks pt
                  WHERE ((pt.session_id = ps.id) AND (pt.status = 3))) AS publish_sucess_track_count,
            c_ou.image_url AS created_user_img,
            rs.station AS station_name,
            concat(c_ou.first_name, ' ', c_ou.last_name) AS created_user,
            ps.signiant_ref_id,
            ps.publish_status,
            ps.publish_attempts,
            ps.publish_start_datetime,
            ps.notification_sent
           FROM ((playout.playout_session ps
             LEFT JOIN public.org_user c_ou ON ((ps.created_by = c_ou.user_id)))
             LEFT JOIN playout.radio_stations rs ON ((ps.station_id = rs.id)))) table1;


ALTER TABLE public.playout_session_search OWNER TO admin;

--
-- Name: playout_session_tracks; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.playout_session_tracks (
    id integer NOT NULL,
    session_id integer NOT NULL,
    track_id uuid NOT NULL,
    type integer NOT NULL,
    status integer NOT NULL,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone NOT NULL,
    created_by integer NOT NULL,
    last_edited_by integer NOT NULL,
    title character varying NOT NULL,
    isrc character varying NOT NULL,
    performer character varying NOT NULL,
    track_type character varying NOT NULL,
    artwork_url character varying
);


ALTER TABLE public.playout_session_tracks OWNER TO admin;

--
-- Name: playout_session_tracks_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.playout_session_tracks_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.playout_session_tracks_id_seq OWNER TO admin;

--
-- Name: playout_session_tracks_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.playout_session_tracks_id_seq OWNED BY public.playout_session_tracks.id;


--
-- Name: playout_tracks_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.playout_tracks_search AS
 SELECT pst.title,
    pst.id,
    pst.track_id,
    pst.track_type,
    pst.performer,
    pst.isrc,
    pst.status,
    pst.created_by,
    pst.date_created,
    pst.date_last_edited,
    pst.session_id,
    pst.artwork_url,
    pst.album_title,
    pst.label,
    pst.dh_track_id,
    ps.last_status AS session_status,
    pst.xml_status,
    pst.asset_status
   FROM (playout.playout_session_tracks pst
     LEFT JOIN playout.playout_session ps ON ((ps.id = pst.session_id)));


ALTER TABLE public.playout_tracks_search OWNER TO admin;

--
-- Name: ppl_label_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.ppl_label_search AS
 SELECT ml.id,
    ml.member,
    ml.label,
    ml.mlc,
    ml.date_created,
    ml.source
   FROM public.member_label ml;


ALTER TABLE public.ppl_label_search OWNER TO admin;

--
-- Name: prior_approval_work; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.prior_approval_work (
    id bigint NOT NULL,
    date_created timestamp without time zone NOT NULL,
    created_by integer NOT NULL,
    date_last_edited timestamp without time zone NOT NULL,
    last_edited_by integer NOT NULL,
    ice_mapping_code character varying,
    local_work_id character varying,
    tunecode character varying,
    iswc character varying,
    work_title character varying,
    composers character varying,
    publisher character varying,
    matched_isrc character varying,
    matched_dh_ids character varying,
    broadcaster character varying,
    artist character varying,
    writers character varying
);


ALTER TABLE public.prior_approval_work OWNER TO admin;

--
-- Name: prior_approval_work_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.prior_approval_work_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.prior_approval_work_id_seq OWNER TO admin;

--
-- Name: prior_approval_work_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.prior_approval_work_id_seq OWNED BY public.prior_approval_work.id;


--
-- Name: radio_stations; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.radio_stations (
    id uuid NOT NULL,
    sys character varying NOT NULL,
    station character varying NOT NULL,
    ingest_area character varying NOT NULL,
    created_by integer NOT NULL,
    date_created timestamp without time zone NOT NULL,
    "order" integer,
    category_id integer
);


ALTER TABLE public.radio_stations OWNER TO admin;

--
-- Name: sync_info; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.sync_info (
    id integer NOT NULL,
    type character varying NOT NULL,
    workspace_id uuid,
    last_synced_date timestamp without time zone,
    date_created timestamp without time zone NOT NULL
);


ALTER TABLE public.sync_info OWNER TO admin;

--
-- Name: sync_info_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.sync_info_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.sync_info_id_seq OWNER TO admin;

--
-- Name: sync_info_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.sync_info_id_seq OWNED BY public.sync_info.id;


--
-- Name: sync_status; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.sync_status (
    status_code character varying(5) NOT NULL,
    status character varying NOT NULL
);


ALTER TABLE public.sync_status OWNER TO admin;

--
-- Name: tag; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.tag (
    tag_id uuid NOT NULL,
    tag_type_id uuid NOT NULL,
    tag_value character varying NOT NULL,
    date_created timestamp without time zone NOT NULL,
    created_by uuid,
    created_organisation_id uuid,
    rating integer,
    tag_code_id uuid,
    date_last_edited timestamp without time zone,
    last_edited_by uuid
);


ALTER TABLE public.tag OWNER TO admin;

--
-- Name: tag_code; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.tag_code (
    tag_code_id integer NOT NULL,
    tag_code character varying,
    code character varying(4)
);


ALTER TABLE public.tag_code OWNER TO admin;

--
-- Name: tag_track; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.tag_track (
    id bigint NOT NULL,
    track_id uuid NOT NULL,
    tag uuid NOT NULL,
    tag_with_type character varying(74) NOT NULL
);


ALTER TABLE public.tag_track OWNER TO admin;

--
-- Name: tag_track_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.tag_track_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.tag_track_id_seq OWNER TO admin;

--
-- Name: tag_track_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.tag_track_id_seq OWNED BY public.tag_track.id;


--
-- Name: tag_type; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.tag_type (
    tag_type_id uuid NOT NULL,
    tag_type character varying NOT NULL
);


ALTER TABLE public.tag_type OWNER TO admin;

--
-- Name: track_org; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.track_org (
    id uuid NOT NULL,
    original_track_id uuid NOT NULL,
    org_id character varying NOT NULL,
    change_log json,
    tags json,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone,
    c_tags json,
    album_id uuid,
    source_deleted boolean,
    restricted boolean,
    archive boolean,
    org_data json,
    created_by integer NOT NULL,
    last_edited_by integer NOT NULL,
    ml_status integer NOT NULL,
    manually_deleted boolean,
    org_workspace_id uuid NOT NULL,
    api_result_id bigint NOT NULL,
    prs_details json,
    chart_info json,
    chart_artist boolean,
    content_alert boolean,
    content_alerted_date timestamp without time zone,
    content_alerted_user integer,
    ca_resolved_date timestamp without time zone,
    ca_resolved_user integer,
    alert_type integer,
    alert_note character varying,
    clearance_track boolean DEFAULT false NOT NULL
);


ALTER TABLE public.track_org OWNER TO admin;

--
-- Name: upload_album; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.upload_album (
    id uuid NOT NULL,
    session_id integer NOT NULL,
    dh_album_id uuid,
    modified boolean,
    artwork_uploaded boolean,
    artist character varying,
    album_name character varying,
    release_date character varying,
    metadata_json json,
    date_created timestamp without time zone NOT NULL,
    created_by integer NOT NULL,
    date_last_edited timestamp without time zone,
    last_edited_by integer,
    catalogue_number character varying,
    artwork character varying,
    rec_type character varying(8),
    copy_source_album_id uuid,
    copy_source_ws_id uuid,
    upload_id uuid
);


ALTER TABLE public.upload_album OWNER TO admin;

--
-- Name: upload_track; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.upload_track (
    id uuid NOT NULL,
    session_id integer NOT NULL,
    track_name character varying,
    size integer,
    status integer,
    s3_id character varying,
    dh_track_id uuid,
    track_type character varying,
    metadata_json json,
    modified boolean,
    asset_uploaded boolean DEFAULT false,
    asset_upload_status character varying,
    asset_upload_begin timestamp without time zone,
    asset_upload_last_check timestamp without time zone,
    date_created timestamp without time zone,
    created_by integer,
    date_last_edited timestamp without time zone,
    last_edited_by integer,
    search_string character varying,
    dh_album_id uuid,
    ml_album_id uuid,
    artwork_uploaded boolean,
    dh_track_metadata json,
    dh_album_metadata json,
    rec_type character varying(8),
    copy_source_track_id uuid,
    copy_source_album_id uuid,
    copy_source_ws_id uuid,
    dh_synced boolean,
    ws_id uuid,
    upload_id uuid,
    archived boolean,
    performer character varying,
    album_name character varying,
    "position" integer,
    isrc character varying,
    iswc character varying,
    file_name character varying,
    duration double precision,
    disc_no integer,
    upload_session_id bigint,
    xml_md5_hash character varying,
    asset_md5_hash character varying
);


ALTER TABLE public.upload_track OWNER TO admin;

--
-- Name: upload_album_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.upload_album_search AS
 SELECT ua2.id,
    ua2.session_id,
    ua2.dh_album_id,
    ua2.modified,
    ua2.artwork_uploaded,
    ua2.artist,
    ua2.album_name,
    ua2.release_date,
    ua2.metadata_json,
    ua2.date_created,
    ua2.created_by,
    ua2.date_last_edited,
    ua2.last_edited_by,
    ua2.catalogue_number,
    ua2.artwork,
    ua2.rec_type,
    ua2.copy_source_album_id,
    ua2.copy_source_ws_id,
    ua2.upload_id,
    concat(c_ou.first_name, ' ', c_ou.last_name) AS created_user,
    concat(e_ou.first_name, ' ', e_ou.last_name) AS edited_user,
    c_ou.image_url AS created_user_img,
    e_ou.image_url AS edited_user_img,
    ( SELECT count(*) AS count
           FROM public.upload_track ut
          WHERE (ut.ml_album_id = ua2.id)) AS track_count
   FROM ((public.upload_album ua2
     LEFT JOIN public.org_user c_ou ON ((ua2.created_by = c_ou.user_id)))
     LEFT JOIN public.org_user e_ou ON ((ua2.last_edited_by = e_ou.user_id)))
  WHERE (ua2.session_id <> 0);


ALTER TABLE public.upload_album_search OWNER TO admin;

--
-- Name: upload_session; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.upload_session (
    id bigint NOT NULL,
    org_id character varying NOT NULL,
    log_date date NOT NULL,
    track_count integer,
    status integer NOT NULL,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone,
    session_name character varying,
    created_by integer
);


ALTER TABLE public.upload_session OWNER TO admin;

--
-- Name: upload_session_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.upload_session_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.upload_session_id_seq OWNER TO admin;

--
-- Name: upload_session_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.upload_session_id_seq OWNED BY public.upload_session.id;


--
-- Name: upload_session_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.upload_session_search AS
 SELECT us.id,
    us.org_id,
    us.log_date,
    us.status,
    us.created_by,
    us.date_created,
    us.date_last_edited,
    concat(c_ou.first_name, ' ', c_ou.last_name) AS created_user,
    c_ou.image_url AS created_user_img,
    ( SELECT count(*) AS count
           FROM public.upload_track ut
          WHERE (((ut.session_id)::bigint = us.id) AND ((ut.rec_type)::text <> 'CREATE'::text))) AS track_count
   FROM (public.upload_session us
     LEFT JOIN public.org_user c_ou ON ((us.created_by = c_ou.user_id)));


ALTER TABLE public.upload_session_search OWNER TO admin;

--
-- Name: upload_tracks_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.upload_tracks_search AS
 SELECT ut.id,
    ut.session_id,
    ut.track_name,
    ut.size,
    ut.status,
    ut.s3_id,
    ut.dh_track_id,
    ut.track_type,
    ut.modified,
    ut.asset_uploaded,
    ut.asset_upload_status,
    ut.asset_upload_begin,
    ut.asset_upload_last_check,
    ut.date_created,
    ut.created_by,
    ut.id AS metadata_json,
    ut.date_last_edited,
    ut.last_edited_by,
    ut.search_string,
    ut.dh_album_id,
    ut.ml_album_id,
    ut.artwork_uploaded,
    ut.dh_synced,
    ut.ws_id,
    ut.performer,
    ua.album_name,
    ut.rec_type,
    concat(c_ou.first_name, ' ', c_ou.last_name) AS created_user,
    concat(e_ou.first_name, ' ', e_ou.last_name) AS edited_user,
    c_ou.image_url AS created_user_img,
    e_ou.image_url AS edited_user_img,
    ut.upload_id,
    ua.upload_id AS album_upload_id,
    ua.artwork,
    ua.catalogue_number,
    ut.archived,
    ut."position",
    ut.isrc,
    ut.iswc,
    ut.file_name,
    ut.duration,
    ut.disc_no,
    ut.upload_session_id,
        CASE
            WHEN (ut.status = 2) THEN 'Upload Success'::text
            ELSE 'Waiting'::text
        END AS disply_status
   FROM (((public.upload_track ut
     LEFT JOIN public.upload_album ua ON ((ut.ml_album_id = ua.id)))
     LEFT JOIN public.org_user c_ou ON ((ut.created_by = c_ou.user_id)))
     LEFT JOIN public.org_user e_ou ON ((ut.last_edited_by = e_ou.user_id)))
  WHERE ((ut.rec_type)::text <> 'CREATE'::text)
  ORDER BY ut.upload_session_id, ut.disc_no, ut."position";


ALTER TABLE public.upload_tracks_search OWNER TO admin;

--
-- Name: workspace_org; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.workspace_org (
    org_workspace_id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    org_id character varying NOT NULL,
    ml_status integer,
    sync_status integer,
    restricted boolean,
    archived boolean,
    notes character varying,
    date_created timestamp without time zone NOT NULL,
    date_last_edited timestamp without time zone NOT NULL,
    created_by integer NOT NULL,
    last_edited_by integer NOT NULL,
    index_status integer,
    album_sync_status integer,
    album_index_status integer,
    last_sync_api_result_id bigint NOT NULL,
    last_album_sync_api_result_id bigint NOT NULL,
    music_origin integer,
    group_ids bigint[]
);


ALTER TABLE public.workspace_org OWNER TO admin;

--
-- Name: workspace_pause; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.workspace_pause (
    id uuid NOT NULL,
    workspace_id uuid NOT NULL,
    date_created timestamp(0) without time zone NOT NULL,
    created_by integer NOT NULL,
    last_download_status integer NOT NULL
);


ALTER TABLE public.workspace_pause OWNER TO admin;

--
-- Name: workspace_search; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.workspace_search AS
 SELECT (w.workspace_id)::text AS id,
    w.workspace_name,
    w.info,
    w.created_by,
    w.dh_status,
    w.restricted,
    w.last_edited_by,
    w.wslib_id,
    w.notes,
    w.download_status,
    w.archived,
    w.date_last_edited,
    w.date_created,
    w.track_count,
    w.ml_track_count,
    w.next_page_token,
    w.last_sync_date,
    wo.ml_status,
    wo.sync_status,
    wo.index_status,
    wo.org_id,
    COALESCE(ow.ws_type, ("left"('External'::text, 10))::character varying) AS type,
    oe.id AS exclude,
    ( SELECT count(*) AS count
           FROM public.library l2
          WHERE ((l2.workspace_id = w.workspace_id) AND (l2.archived = false))) AS library_count,
    wo.group_ids
   FROM (((public.workspace w
     LEFT JOIN public.org_workspace ow ON ((w.workspace_id = ow.dh_ws_id)))
     LEFT JOIN public.workspace_org wo ON ((w.workspace_id = wo.workspace_id)))
     LEFT JOIN public.org_exclude oe ON ((w.workspace_id = oe.ref_id)));


ALTER TABLE public.workspace_search OWNER TO admin;

--
-- Name: workspace_search_ml_admin; Type: VIEW; Schema: public; Owner: admin
--

CREATE VIEW public.workspace_search_ml_admin AS
 SELECT (w.workspace_id)::text AS id,
    wo.org_workspace_id AS owsid,
    w.workspace_name,
    w.info,
    w.created_by,
    w.dh_status,
    w.restricted,
    w.last_edited_by,
    w.wslib_id,
    w.notes,
    w.archived,
    w.date_last_edited,
    w.date_created,
    w.track_count,
    w.ml_track_count,
    w.next_page_token,
    w.last_sync_date,
    wo.ml_status,
    wo.sync_status,
    wo.music_origin,
    wo.index_status,
    wo.org_id,
    w.download_status,
    wo.group_ids,
    COALESCE(ow.ws_type, ("left"('External'::text, 10))::character varying) AS type,
    ( SELECT count(*) AS count
           FROM public.library l2
          WHERE ((l2.workspace_id = w.workspace_id) AND (l2.archived = false))) AS library_count
   FROM ((public.workspace w
     LEFT JOIN public.org_workspace ow ON ((w.workspace_id = ow.dh_ws_id)))
     LEFT JOIN public.workspace_org wo ON ((w.workspace_id = wo.workspace_id)))
  WHERE (wo.org_id IS NOT NULL);


ALTER TABLE public.workspace_search_ml_admin OWNER TO admin;

--
-- Name: ws_lib_tracks_to_be_synced; Type: TABLE; Schema: public; Owner: admin
--

CREATE TABLE public.ws_lib_tracks_to_be_synced (
    id bigint NOT NULL,
    type character varying(3) NOT NULL,
    ref_id uuid NOT NULL,
    status character varying(8) NOT NULL,
    available_from date,
    date_created timestamp without time zone NOT NULL,
    created_by integer NOT NULL,
    elastic_status character varying(5),
    counts_updated boolean,
    album_indexed boolean,
    date_last_edited timestamp without time zone,
    reindex_ref uuid,
    org_id character varying
);


ALTER TABLE public.ws_lib_tracks_to_be_synced OWNER TO admin;

--
-- Name: ws_lib_tracks_to_be_synced_id_seq; Type: SEQUENCE; Schema: public; Owner: admin
--

CREATE SEQUENCE public.ws_lib_tracks_to_be_synced_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.ws_lib_tracks_to_be_synced_id_seq OWNER TO admin;

--
-- Name: ws_lib_tracks_to_be_synced_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: admin
--

ALTER SEQUENCE public.ws_lib_tracks_to_be_synced_id_seq OWNED BY public.ws_lib_tracks_to_be_synced.id;


--
-- Name: isrc_tunecode; Type: TABLE; Schema: staging; Owner: admin
--

CREATE TABLE staging.isrc_tunecode (
    tunecode character varying NOT NULL,
    isrc character varying
);


ALTER TABLE staging.isrc_tunecode OWNER TO admin;

--
-- Name: staging_library; Type: TABLE; Schema: staging; Owner: admin
--

CREATE TABLE staging.staging_library (
    library_id uuid NOT NULL,
    library_name character varying,
    workspace_id uuid NOT NULL,
    track_count integer NOT NULL,
    date_created bigint,
    created_by integer,
    dh_status character varying(5),
    ml_status character varying(5),
    notes character varying,
    sync_status character varying(5),
    deleted boolean NOT NULL
);


ALTER TABLE staging.staging_library OWNER TO admin;

--
-- Name: staging_tag_track; Type: TABLE; Schema: staging; Owner: admin
--

CREATE TABLE staging.staging_tag_track (
    id bigint NOT NULL,
    track_id uuid NOT NULL,
    tags json
);


ALTER TABLE staging.staging_tag_track OWNER TO admin;

--
-- Name: staging_tag_track_id_seq; Type: SEQUENCE; Schema: staging; Owner: admin
--

CREATE SEQUENCE staging.staging_tag_track_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE staging.staging_tag_track_id_seq OWNER TO admin;

--
-- Name: staging_tag_track_id_seq; Type: SEQUENCE OWNED BY; Schema: staging; Owner: admin
--

ALTER SEQUENCE staging.staging_tag_track_id_seq OWNED BY staging.staging_tag_track.id;


--
-- Name: staging_workspace; Type: TABLE; Schema: staging; Owner: admin
--

CREATE TABLE staging.staging_workspace (
    workspace_id uuid NOT NULL,
    workspace_name character varying,
    track_count integer,
    deleted boolean NOT NULL,
    date_created character varying,
    date_content_modified character varying
);


ALTER TABLE staging.staging_workspace OWNER TO admin;

--
-- Name: chart_sync_summary id; Type: DEFAULT; Schema: charts; Owner: admin
--

ALTER TABLE ONLY charts.chart_sync_summary ALTER COLUMN id SET DEFAULT nextval('charts.chart_sync_summary_id_seq'::regclass);


--
-- Name: elastic_track_change id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.elastic_track_change ALTER COLUMN id SET DEFAULT nextval('log.elastic_track_change_id_seq'::regclass);


--
-- Name: log_album_api_calls id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_album_api_calls ALTER COLUMN id SET DEFAULT nextval('log.log_album_api_calls_id_seq'::regclass);


--
-- Name: log_album_api_results id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_album_api_results ALTER COLUMN id SET DEFAULT nextval('log.log_album_api_results_id_seq'::regclass);


--
-- Name: log_album_sync_session session_id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_album_sync_session ALTER COLUMN session_id SET DEFAULT nextval('log.log_album_sync_session_session_id_seq'::regclass);


--
-- Name: log_elastic_track_changes id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_elastic_track_changes ALTER COLUMN id SET DEFAULT nextval('log.log_elastic_track_changes_id_seq'::regclass);


--
-- Name: log_library_change libch_id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_library_change ALTER COLUMN libch_id SET DEFAULT nextval('log.log_library_change_libch_id_seq'::regclass);


--
-- Name: log_prs_search_time id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_prs_search_time ALTER COLUMN id SET DEFAULT nextval('log.log_prs_search_time_id_seq'::regclass);


--
-- Name: log_sync_time id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_sync_time ALTER COLUMN id SET DEFAULT nextval('log.log_sync_time_id_seq'::regclass);


--
-- Name: log_track_api_calls id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_track_api_calls ALTER COLUMN id SET DEFAULT nextval('log.log_track_api_calls_id_seq'::regclass);


--
-- Name: log_track_index_error id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_track_index_error ALTER COLUMN id SET DEFAULT nextval('log.log_track_index_error_id_seq'::regclass);


--
-- Name: log_track_sync_session session_id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_track_sync_session ALTER COLUMN session_id SET DEFAULT nextval('log.log_track_sync_session_session_id_seq'::regclass);


--
-- Name: log_user_action id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_user_action ALTER COLUMN id SET DEFAULT nextval('log.log_user_action_id_seq'::regclass);


--
-- Name: log_workspace_change wsch_id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_workspace_change ALTER COLUMN wsch_id SET DEFAULT nextval('log.log_workspace_change_wsch_id_seq'::regclass);


--
-- Name: log_ws_lib_change ws_lib_change_id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_ws_lib_change ALTER COLUMN ws_lib_change_id SET DEFAULT nextval('log.ws_lib_change_ws_lib_change_id_seq'::regclass);


--
-- Name: log_ws_lib_status_change id; Type: DEFAULT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_ws_lib_status_change ALTER COLUMN id SET DEFAULT nextval('log.log_ws_lib_status_change_id_seq'::regclass);


--
-- Name: playout_response response_id; Type: DEFAULT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.playout_response ALTER COLUMN response_id SET DEFAULT nextval('playout.playout_response_response_id_seq'::regclass);


--
-- Name: playout_response_status id; Type: DEFAULT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.playout_response_status ALTER COLUMN id SET DEFAULT nextval('playout.playout_response_status_id_seq'::regclass);


--
-- Name: playout_session id; Type: DEFAULT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.playout_session ALTER COLUMN id SET DEFAULT nextval('playout.playout_session_id_seq'::regclass);


--
-- Name: playout_session_tracks id; Type: DEFAULT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.playout_session_tracks ALTER COLUMN id SET DEFAULT nextval('playout.playout_session_tracks_id_seq'::regclass);


--
-- Name: c_tag id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.c_tag ALTER COLUMN id SET DEFAULT nextval('public.c_tag_id_seq'::regclass);


--
-- Name: c_tag_extended id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.c_tag_extended ALTER COLUMN id SET DEFAULT nextval('public.c_tag_extended_id_seq'::regclass);


--
-- Name: cleansed_tag_track id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.cleansed_tag_track ALTER COLUMN id SET DEFAULT nextval('public.cleansed_tag_track_id_seq'::regclass);


--
-- Name: member_label id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.member_label ALTER COLUMN id SET DEFAULT nextval('public.member_label_id_seq'::regclass);


--
-- Name: org_exclude id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.org_exclude ALTER COLUMN id SET DEFAULT nextval('public.org_exclude_id_seq'::regclass);


--
-- Name: org_workspace id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.org_workspace ALTER COLUMN id SET DEFAULT nextval('public.org_workspace_id_seq'::regclass);


--
-- Name: playout_session id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.playout_session ALTER COLUMN id SET DEFAULT nextval('public.playout_session_id_seq'::regclass);


--
-- Name: playout_session_tracks id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.playout_session_tracks ALTER COLUMN id SET DEFAULT nextval('public.playout_session_tracks_id_seq'::regclass);


--
-- Name: prior_approval_work id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.prior_approval_work ALTER COLUMN id SET DEFAULT nextval('public.prior_approval_work_id_seq'::regclass);


--
-- Name: sync_info id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.sync_info ALTER COLUMN id SET DEFAULT nextval('public.sync_info_id_seq'::regclass);


--
-- Name: tag_track id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.tag_track ALTER COLUMN id SET DEFAULT nextval('public.tag_track_id_seq'::regclass);


--
-- Name: upload_session id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.upload_session ALTER COLUMN id SET DEFAULT nextval('public.upload_session_id_seq'::regclass);


--
-- Name: ws_lib_tracks_to_be_synced id; Type: DEFAULT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.ws_lib_tracks_to_be_synced ALTER COLUMN id SET DEFAULT nextval('public.ws_lib_tracks_to_be_synced_id_seq'::regclass);


--
-- Name: staging_tag_track id; Type: DEFAULT; Schema: staging; Owner: admin
--

ALTER TABLE ONLY staging.staging_tag_track ALTER COLUMN id SET DEFAULT nextval('staging.staging_tag_track_id_seq'::regclass);


--
-- Name: chart_master_albums chart_master_albums_pkey; Type: CONSTRAINT; Schema: charts; Owner: admin
--

ALTER TABLE ONLY charts.chart_master_albums
    ADD CONSTRAINT chart_master_albums_pkey PRIMARY KEY (master_id);


--
-- Name: chart_master_tracks chart_master_tracks_pkey; Type: CONSTRAINT; Schema: charts; Owner: admin
--

ALTER TABLE ONLY charts.chart_master_tracks
    ADD CONSTRAINT chart_master_tracks_pkey PRIMARY KEY (master_id);


--
-- Name: chart_sync_summary sync_summary_pkey; Type: CONSTRAINT; Schema: charts; Owner: admin
--

ALTER TABLE ONLY charts.chart_sync_summary
    ADD CONSTRAINT sync_summary_pkey PRIMARY KEY (id);


--
-- Name: elastic_album_change elastic_album_change_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.elastic_album_change
    ADD CONSTRAINT elastic_album_change_pkey PRIMARY KEY (document_id);


--
-- Name: elastic_track_change elastic_track_change_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.elastic_track_change
    ADD CONSTRAINT elastic_track_change_pkey PRIMARY KEY (id);


--
-- Name: log_album_api_calls log_album_api_calls_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_album_api_calls
    ADD CONSTRAINT log_album_api_calls_pkey PRIMARY KEY (id);


--
-- Name: log_album_api_results log_album_api_results_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_album_api_results
    ADD CONSTRAINT log_album_api_results_pkey PRIMARY KEY (id);


--
-- Name: log_album_sync_session log_album_sync_session_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_album_sync_session
    ADD CONSTRAINT log_album_sync_session_pkey PRIMARY KEY (session_id);


--
-- Name: log_elastic_track_changes log_elastic_track_changes_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_elastic_track_changes
    ADD CONSTRAINT log_elastic_track_changes_pkey PRIMARY KEY (id);


--
-- Name: log_library_change log_library_change_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_library_change
    ADD CONSTRAINT log_library_change_pkey PRIMARY KEY (libch_id);


--
-- Name: log_prs_search_time log_prs_search_time_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_prs_search_time
    ADD CONSTRAINT log_prs_search_time_pkey PRIMARY KEY (id);


--
-- Name: log_sync_time log_sync_time_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_sync_time
    ADD CONSTRAINT log_sync_time_pkey PRIMARY KEY (id);


--
-- Name: log_track_api_calls log_track_api_calls_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_track_api_calls
    ADD CONSTRAINT log_track_api_calls_pkey PRIMARY KEY (id);


--
-- Name: log_track_api_results_20240301 log_track_api_results_20240301_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_track_api_results_20240301
    ADD CONSTRAINT log_track_api_results_20240301_pkey PRIMARY KEY (id);


--
-- Name: log_track_api_results log_track_api_results_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_track_api_results
    ADD CONSTRAINT log_track_api_results_pkey PRIMARY KEY (id);


--
-- Name: log_track_index_error log_track_index_error_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_track_index_error
    ADD CONSTRAINT log_track_index_error_pkey PRIMARY KEY (id);


--
-- Name: log_track_sync_session log_track_sync_session_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_track_sync_session
    ADD CONSTRAINT log_track_sync_session_pkey PRIMARY KEY (session_id);


--
-- Name: log_user_action log_user_action_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_user_action
    ADD CONSTRAINT log_user_action_pkey PRIMARY KEY (id);


--
-- Name: log_workspace_change log_workspace_change_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_workspace_change
    ADD CONSTRAINT log_workspace_change_pkey PRIMARY KEY (wsch_id);


--
-- Name: log_ws_lib_status_change log_ws_lib_status_change_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_ws_lib_status_change
    ADD CONSTRAINT log_ws_lib_status_change_pkey PRIMARY KEY (id);


--
-- Name: log_ws_lib_change ws_lib_change_pkey; Type: CONSTRAINT; Schema: log; Owner: admin
--

ALTER TABLE ONLY log.log_ws_lib_change
    ADD CONSTRAINT ws_lib_change_pkey PRIMARY KEY (ws_lib_change_id);


--
-- Name: playout_response playout_response_pkey; Type: CONSTRAINT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.playout_response
    ADD CONSTRAINT playout_response_pkey PRIMARY KEY (response_id);


--
-- Name: playout_response_status playout_response_status_pk; Type: CONSTRAINT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.playout_response_status
    ADD CONSTRAINT playout_response_status_pk PRIMARY KEY (id);


--
-- Name: playout_session playout_session_pkey; Type: CONSTRAINT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.playout_session
    ADD CONSTRAINT playout_session_pkey PRIMARY KEY (id);


--
-- Name: playout_session_tracks playout_session_tracks_pkey; Type: CONSTRAINT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.playout_session_tracks
    ADD CONSTRAINT playout_session_tracks_pkey PRIMARY KEY (id);


--
-- Name: radio_categories radio_categories_pk; Type: CONSTRAINT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.radio_categories
    ADD CONSTRAINT radio_categories_pk PRIMARY KEY (category_id);


--
-- Name: radio_stations radio_stations_pkey; Type: CONSTRAINT; Schema: playout; Owner: admin
--

ALTER TABLE ONLY playout.radio_stations
    ADD CONSTRAINT radio_stations_pkey PRIMARY KEY (id);


--
-- Name: album_org album_org_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.album_org
    ADD CONSTRAINT album_org_pkey PRIMARY KEY (original_album_id, org_id);


--
-- Name: c_tag_extended c_tag_extended_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.c_tag_extended
    ADD CONSTRAINT c_tag_extended_pkey PRIMARY KEY (id);


--
-- Name: c_tag_index_status c_tag_index_status_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.c_tag_index_status
    ADD CONSTRAINT c_tag_index_status_pkey PRIMARY KEY (type);


--
-- Name: c_tag c_tag_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.c_tag
    ADD CONSTRAINT c_tag_pkey PRIMARY KEY (id);


--
-- Name: cleansed_tag_track cleansed_tag_track_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.cleansed_tag_track
    ADD CONSTRAINT cleansed_tag_track_pkey PRIMARY KEY (id);


--
-- Name: library_org library_org_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.library_org
    ADD CONSTRAINT library_org_pkey PRIMARY KEY (org_library_id);


--
-- Name: library library_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.library
    ADD CONSTRAINT library_pkey PRIMARY KEY (library_id);


--
-- Name: member_label member_label_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.member_label
    ADD CONSTRAINT member_label_pkey PRIMARY KEY (id);


--
-- Name: ml_master_album ml_master_album_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.ml_master_album
    ADD CONSTRAINT ml_master_album_pkey PRIMARY KEY (album_id);


--
-- Name: ml_master_track ml_master_track_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.ml_master_track
    ADD CONSTRAINT ml_master_track_pkey PRIMARY KEY (track_id);


--
-- Name: org_exclude org_exclude_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.org_exclude
    ADD CONSTRAINT org_exclude_pkey PRIMARY KEY (id);


--
-- Name: org_track_version org_track_version_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.org_track_version
    ADD CONSTRAINT org_track_version_pkey PRIMARY KEY (ml_version_id);


--
-- Name: org_user org_user_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.org_user
    ADD CONSTRAINT org_user_pkey PRIMARY KEY (user_id);


--
-- Name: org_workspace org_workspace_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.org_workspace
    ADD CONSTRAINT org_workspace_pkey PRIMARY KEY (id);


--
-- Name: playout_session playout_session_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.playout_session
    ADD CONSTRAINT playout_session_pkey PRIMARY KEY (id);


--
-- Name: playout_session_tracks playout_session_tracks_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.playout_session_tracks
    ADD CONSTRAINT playout_session_tracks_pkey PRIMARY KEY (id);


--
-- Name: prior_approval_work prior_approval_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.prior_approval_work
    ADD CONSTRAINT prior_approval_pkey PRIMARY KEY (id);


--
-- Name: radio_stations radio_stations_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.radio_stations
    ADD CONSTRAINT radio_stations_pkey PRIMARY KEY (id);


--
-- Name: sync_info sync_info_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.sync_info
    ADD CONSTRAINT sync_info_pkey PRIMARY KEY (id);


--
-- Name: sync_status sync_status_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.sync_status
    ADD CONSTRAINT sync_status_pkey PRIMARY KEY (status_code);


--
-- Name: tag_code tag_code_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.tag_code
    ADD CONSTRAINT tag_code_pkey PRIMARY KEY (tag_code_id);


--
-- Name: tag tag_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.tag
    ADD CONSTRAINT tag_pkey PRIMARY KEY (tag_id);


--
-- Name: tag_track tag_track_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.tag_track
    ADD CONSTRAINT tag_track_pkey PRIMARY KEY (id);


--
-- Name: tag_type tag_type_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.tag_type
    ADD CONSTRAINT tag_type_pkey PRIMARY KEY (tag_type_id);


--
-- Name: track_org track_org_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.track_org
    ADD CONSTRAINT track_org_pkey PRIMARY KEY (original_track_id, org_id);


--
-- Name: upload_album upload_album_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.upload_album
    ADD CONSTRAINT upload_album_pkey PRIMARY KEY (id);


--
-- Name: upload_session upload_session_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.upload_session
    ADD CONSTRAINT upload_session_pkey PRIMARY KEY (id);


--
-- Name: upload_track upload_track_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.upload_track
    ADD CONSTRAINT upload_track_pkey PRIMARY KEY (id);


--
-- Name: workspace_org workspace_org_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.workspace_org
    ADD CONSTRAINT workspace_org_pkey PRIMARY KEY (org_workspace_id);


--
-- Name: workspace_pause workspace_pause_pk; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.workspace_pause
    ADD CONSTRAINT workspace_pause_pk PRIMARY KEY (id);


--
-- Name: workspace workspace_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.workspace
    ADD CONSTRAINT workspace_pkey PRIMARY KEY (workspace_id);


--
-- Name: ws_lib_tracks_to_be_synced ws_lib_tracks_to_be_synced_pkey; Type: CONSTRAINT; Schema: public; Owner: admin
--

ALTER TABLE ONLY public.ws_lib_tracks_to_be_synced
    ADD CONSTRAINT ws_lib_tracks_to_be_synced_pkey PRIMARY KEY (id);


--
-- Name: staging_library library_pkey; Type: CONSTRAINT; Schema: staging; Owner: admin
--

ALTER TABLE ONLY staging.staging_library
    ADD CONSTRAINT library_pkey PRIMARY KEY (library_id);


--
-- Name: staging_tag_track staging_tag_track_pkey; Type: CONSTRAINT; Schema: staging; Owner: admin
--

ALTER TABLE ONLY staging.staging_tag_track
    ADD CONSTRAINT staging_tag_track_pkey PRIMARY KEY (id);


--
-- Name: staging_workspace workspace_pkey; Type: CONSTRAINT; Schema: staging; Owner: admin
--

ALTER TABLE ONLY staging.staging_workspace
    ADD CONSTRAINT workspace_pkey PRIMARY KEY (workspace_id);


--
-- Name: idx_org_workspace_id; Type: INDEX; Schema: log; Owner: admin
--

CREATE INDEX idx_org_workspace_id ON log.elastic_track_change USING btree (org_workspace_id);


--
-- Name: idx_original_track_id_org_id; Type: INDEX; Schema: log; Owner: admin
--

CREATE INDEX idx_original_track_id_org_id ON log.elastic_track_change USING btree (original_track_id, org_id);


--
-- Name: idx_album_id; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX idx_album_id ON public.track_org USING btree (album_id);


--
-- Name: idx_member_label; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX idx_member_label ON public.member_label USING btree (member, label);


--
-- Name: idx_org_workspace_id; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX idx_org_workspace_id ON public.track_org USING btree (org_workspace_id);


--
-- Name: idx_original_album_id; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX idx_original_album_id ON public.album_org USING btree (original_album_id);


--
-- Name: idx_original_track_id; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX idx_original_track_id ON public.track_org USING btree (original_track_id);


--
-- Name: idx_upload_id; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX idx_upload_id ON public.upload_track USING btree (session_id);


--
-- Name: idx_workspace_id; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX idx_workspace_id ON public.workspace_org USING btree (workspace_id);


--
-- Name: library_ws_idx; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX library_ws_idx ON public.library USING btree (workspace_id);


--
-- Name: ml_master_album_id_idx; Type: INDEX; Schema: public; Owner: admin
--

CREATE UNIQUE INDEX ml_master_album_id_idx ON public.ml_master_album USING btree (album_id);


--
-- Name: ml_master_album_workspace_id_library_id_api_result_id; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX ml_master_album_workspace_id_library_id_api_result_id ON public.ml_master_album USING btree (workspace_id, library_id, api_result_id);


--
-- Name: ml_master_track_album_id_idx; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX ml_master_track_album_id_idx ON public.ml_master_track USING btree (album_id);


--
-- Name: ml_master_track_deleted; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX ml_master_track_deleted ON public.ml_master_track USING btree (deleted);


--
-- Name: ml_master_track_id_idx; Type: INDEX; Schema: public; Owner: admin
--

CREATE UNIQUE INDEX ml_master_track_id_idx ON public.ml_master_track USING btree (track_id);


--
-- Name: ml_master_track_library_id_idx; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX ml_master_track_library_id_idx ON public.ml_master_track USING btree (library_id);


--
-- Name: ml_master_track_workspace_id_api_result_id; Type: INDEX; Schema: public; Owner: admin
--

CREATE INDEX ml_master_track_workspace_id_api_result_id ON public.ml_master_track USING btree (workspace_id, api_result_id);


--
-- Name: log_album_api_results tr_sync_albums; Type: TRIGGER; Schema: log; Owner: admin
--

CREATE TRIGGER tr_sync_albums AFTER INSERT ON log.log_album_api_results FOR EACH ROW EXECUTE FUNCTION public.fn_trigger_sync_album();


--
-- Name: log_track_api_results tr_sync_tracks; Type: TRIGGER; Schema: log; Owner: admin
--

CREATE TRIGGER tr_sync_tracks AFTER INSERT ON log.log_track_api_results FOR EACH ROW EXECUTE FUNCTION public.fn_trigger_sync_track();


--
-- Name: log_track_api_results_20240301 tr_sync_tracks; Type: TRIGGER; Schema: log; Owner: admin
--

CREATE TRIGGER tr_sync_tracks AFTER INSERT ON log.log_track_api_results_20240301 FOR EACH ROW EXECUTE FUNCTION public.fn_trigger_sync_track();


--
-- Name: ml_master_track tr_update_album; Type: TRIGGER; Schema: public; Owner: admin
--

CREATE TRIGGER tr_update_album AFTER INSERT OR UPDATE ON public.ml_master_track FOR EACH ROW EXECUTE FUNCTION public.fn_update_album();


--
-- Name: album_org tr_update_elastic_album_change; Type: TRIGGER; Schema: public; Owner: admin
--

CREATE TRIGGER tr_update_elastic_album_change AFTER INSERT OR UPDATE ON public.album_org FOR EACH ROW EXECUTE FUNCTION public.fn_update_elastic_album_change();


--
-- Name: track_org tr_update_elastic_track_change; Type: TRIGGER; Schema: public; Owner: admin
--

CREATE TRIGGER tr_update_elastic_track_change AFTER INSERT OR UPDATE ON public.track_org FOR EACH ROW EXECUTE FUNCTION public.fn_update_elastic_track_change();


--
-- Name: SCHEMA charts; Type: ACL; Schema: -; Owner: admin
--

GRANT ALL ON SCHEMA charts TO admin;


--
-- Name: SCHEMA log; Type: ACL; Schema: -; Owner: admin
--

GRANT ALL ON SCHEMA log TO admin;


--
-- Name: SCHEMA playout; Type: ACL; Schema: -; Owner: admin
--

GRANT ALL ON SCHEMA playout TO admin;


--
-- Name: SCHEMA public; Type: ACL; Schema: -; Owner: postgres
--

GRANT ALL ON SCHEMA public TO admin;


--
-- Name: SCHEMA staging; Type: ACL; Schema: -; Owner: admin
--

GRANT ALL ON SCHEMA staging TO admin;


--
-- Name: FUNCTION fn_search_elastic_album_changes(pagesize integer, orgworkspaceid uuid); Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON FUNCTION log.fn_search_elastic_album_changes(pagesize integer, orgworkspaceid uuid) TO admin;


--
-- Name: FUNCTION fn_search_elastic_track_changes(pageno integer, pagesize integer, orgworkspaceid uuid); Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON FUNCTION log.fn_search_elastic_track_changes(pageno integer, pagesize integer, orgworkspaceid uuid) TO admin;


--
-- Name: FUNCTION fn_lib_change_trigger(); Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON FUNCTION public.fn_lib_change_trigger() TO admin;


--
-- Name: FUNCTION fn_sync_track(OUT status integer); Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON FUNCTION public.fn_sync_track(OUT status integer) TO admin;


--
-- Name: FUNCTION fn_trigger_sync_album(); Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON FUNCTION public.fn_trigger_sync_album() TO admin;


--
-- Name: FUNCTION fn_trigger_sync_track(); Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON FUNCTION public.fn_trigger_sync_track() TO admin;


--
-- Name: FUNCTION fn_update_album(); Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON FUNCTION public.fn_update_album() TO admin;


--
-- Name: FUNCTION fn_update_elastic_album_change(); Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON FUNCTION public.fn_update_elastic_album_change() TO admin;


--
-- Name: FUNCTION fn_update_elastic_track_change(); Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON FUNCTION public.fn_update_elastic_track_change() TO admin;


--
-- Name: FUNCTION fn_update_tag(_tag_type_id uuid, _track_id uuid, _tag_list character varying); Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON FUNCTION public.fn_update_tag(_tag_type_id uuid, _track_id uuid, _tag_list character varying) TO admin;


--
-- Name: FUNCTION fn_ws_change_trigger(); Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON FUNCTION public.fn_ws_change_trigger() TO admin;


--
-- Name: TABLE chart_master_albums; Type: ACL; Schema: charts; Owner: admin
--

GRANT ALL ON TABLE charts.chart_master_albums TO admin;


--
-- Name: TABLE chart_master_tracks; Type: ACL; Schema: charts; Owner: admin
--

GRANT ALL ON TABLE charts.chart_master_tracks TO admin;


--
-- Name: TABLE chart_sync_summary; Type: ACL; Schema: charts; Owner: admin
--

GRANT ALL ON TABLE charts.chart_sync_summary TO admin;


--
-- Name: SEQUENCE chart_sync_summary_id_seq; Type: ACL; Schema: charts; Owner: admin
--

GRANT ALL ON SEQUENCE charts.chart_sync_summary_id_seq TO admin;


--
-- Name: TABLE elastic_album_change; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.elastic_album_change TO admin;


--
-- Name: TABLE elastic_track_change; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.elastic_track_change TO admin;


--
-- Name: SEQUENCE elastic_track_change_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.elastic_track_change_id_seq TO admin;


--
-- Name: TABLE log_album_api_calls; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_album_api_calls TO admin;


--
-- Name: SEQUENCE log_album_api_calls_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_album_api_calls_id_seq TO admin;


--
-- Name: TABLE log_album_api_results; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_album_api_results TO admin;


--
-- Name: SEQUENCE log_album_api_results_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_album_api_results_id_seq TO admin;


--
-- Name: TABLE log_album_sync_session; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_album_sync_session TO admin;


--
-- Name: SEQUENCE log_album_sync_session_session_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_album_sync_session_session_id_seq TO admin;


--
-- Name: TABLE log_elastic_track_changes; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_elastic_track_changes TO admin;


--
-- Name: SEQUENCE log_elastic_track_changes_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_elastic_track_changes_id_seq TO admin;


--
-- Name: TABLE log_library_change; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_library_change TO admin;


--
-- Name: SEQUENCE log_library_change_libch_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_library_change_libch_id_seq TO admin;


--
-- Name: TABLE log_prs_search_time; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_prs_search_time TO admin;


--
-- Name: SEQUENCE log_prs_search_time_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_prs_search_time_id_seq TO admin;


--
-- Name: TABLE log_sync_time; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_sync_time TO admin;


--
-- Name: SEQUENCE log_sync_time_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_sync_time_id_seq TO admin;


--
-- Name: TABLE log_track_api_calls; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_track_api_calls TO admin;


--
-- Name: SEQUENCE log_track_api_calls_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_track_api_calls_id_seq TO admin;


--
-- Name: TABLE log_track_api_results_20240301; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_track_api_results_20240301 TO admin;


--
-- Name: SEQUENCE log_track_api_results_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_track_api_results_id_seq TO admin;


--
-- Name: TABLE log_track_api_results; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_track_api_results TO admin;


--
-- Name: TABLE log_track_index_error; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_track_index_error TO admin;


--
-- Name: SEQUENCE log_track_index_error_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_track_index_error_id_seq TO admin;


--
-- Name: TABLE log_track_sync_session; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_track_sync_session TO admin;


--
-- Name: SEQUENCE log_track_sync_session_session_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_track_sync_session_session_id_seq TO admin;


--
-- Name: TABLE log_user_action; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_user_action TO admin;


--
-- Name: SEQUENCE log_user_action_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_user_action_id_seq TO admin;


--
-- Name: TABLE log_workspace_change; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_workspace_change TO admin;


--
-- Name: SEQUENCE log_workspace_change_wsch_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_workspace_change_wsch_id_seq TO admin;


--
-- Name: TABLE log_ws_lib_change; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_ws_lib_change TO admin;


--
-- Name: TABLE log_ws_lib_status_change; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON TABLE log.log_ws_lib_status_change TO admin;


--
-- Name: SEQUENCE log_ws_lib_status_change_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.log_ws_lib_status_change_id_seq TO admin;


--
-- Name: SEQUENCE ws_lib_change_ws_lib_change_id_seq; Type: ACL; Schema: log; Owner: admin
--

GRANT ALL ON SEQUENCE log.ws_lib_change_ws_lib_change_id_seq TO admin;


--
-- Name: TABLE playout_response; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON TABLE playout.playout_response TO admin;


--
-- Name: SEQUENCE playout_response_response_id_seq; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON SEQUENCE playout.playout_response_response_id_seq TO admin;


--
-- Name: TABLE playout_response_status; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON TABLE playout.playout_response_status TO admin;


--
-- Name: SEQUENCE playout_response_status_id_seq; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON SEQUENCE playout.playout_response_status_id_seq TO admin;


--
-- Name: TABLE playout_session; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON TABLE playout.playout_session TO admin;


--
-- Name: SEQUENCE playout_session_id_seq; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON SEQUENCE playout.playout_session_id_seq TO admin;


--
-- Name: TABLE radio_stations; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON TABLE playout.radio_stations TO admin;


--
-- Name: TABLE org_user; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.org_user TO admin;


--
-- Name: TABLE playout_session_search; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON TABLE playout.playout_session_search TO admin;


--
-- Name: TABLE playout_session_tracks; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON TABLE playout.playout_session_tracks TO admin;


--
-- Name: SEQUENCE playout_session_tracks_id_seq; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON SEQUENCE playout.playout_session_tracks_id_seq TO admin;


--
-- Name: TABLE radio_categories; Type: ACL; Schema: playout; Owner: admin
--

GRANT ALL ON TABLE playout.radio_categories TO admin;


--
-- Name: TABLE album_org; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.album_org TO admin;


--
-- Name: TABLE c_tag; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.c_tag TO admin;


--
-- Name: TABLE c_tag_extended; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.c_tag_extended TO admin;


--
-- Name: SEQUENCE c_tag_extended_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.c_tag_extended_id_seq TO admin;


--
-- Name: SEQUENCE c_tag_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.c_tag_id_seq TO admin;


--
-- Name: TABLE c_tag_index_status; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.c_tag_index_status TO admin;


--
-- Name: TABLE cleansed_tag_track; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.cleansed_tag_track TO admin;


--
-- Name: SEQUENCE cleansed_tag_track_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.cleansed_tag_track_id_seq TO admin;


--
-- Name: TABLE ctag_extended_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.ctag_extended_search TO admin;


--
-- Name: TABLE ctag_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.ctag_search TO admin;


--
-- Name: TABLE dh_status; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.dh_status TO admin;


--
-- Name: TABLE library; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.library TO admin;


--
-- Name: TABLE library_org; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.library_org TO admin;


--
-- Name: TABLE workspace; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.workspace TO admin;


--
-- Name: TABLE library_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.library_search TO admin;


--
-- Name: TABLE library_search_ml_admin; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.library_search_ml_admin TO admin;


--
-- Name: TABLE member_label; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.member_label TO admin;


--
-- Name: SEQUENCE member_label_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.member_label_id_seq TO admin;


--
-- Name: TABLE ml_master_album; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.ml_master_album TO admin;


--
-- Name: TABLE ml_master_track; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.ml_master_track TO admin;


--
-- Name: TABLE ml_status; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.ml_status TO admin;


--
-- Name: TABLE org_exclude; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.org_exclude TO admin;


--
-- Name: SEQUENCE org_exclude_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.org_exclude_id_seq TO admin;


--
-- Name: TABLE org_track_version; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.org_track_version TO admin;

--
-- Name: TABLE org_workspace; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.org_workspace TO admin;


--
-- Name: SEQUENCE org_workspace_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.org_workspace_id_seq TO admin;


--
-- Name: TABLE playout_session; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.playout_session TO admin;


--
-- Name: SEQUENCE playout_session_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.playout_session_id_seq TO admin;


--
-- Name: TABLE playout_session_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.playout_session_search TO admin;


--
-- Name: TABLE playout_session_tracks; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.playout_session_tracks TO admin;


--
-- Name: SEQUENCE playout_session_tracks_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.playout_session_tracks_id_seq TO admin;


--
-- Name: TABLE playout_tracks_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.playout_tracks_search TO admin;


--
-- Name: TABLE ppl_label_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.ppl_label_search TO admin;


--
-- Name: TABLE prior_approval_work; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.prior_approval_work TO admin;


--
-- Name: SEQUENCE prior_approval_work_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.prior_approval_work_id_seq TO admin;


--
-- Name: TABLE radio_stations; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.radio_stations TO admin;


--
-- Name: TABLE sync_info; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.sync_info TO admin;


--
-- Name: SEQUENCE sync_info_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.sync_info_id_seq TO admin;


--
-- Name: TABLE sync_status; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.sync_status TO admin;


--
-- Name: TABLE tag; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.tag TO admin;


--
-- Name: TABLE tag_code; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.tag_code TO admin;


--
-- Name: TABLE tag_track; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.tag_track TO admin;


--
-- Name: SEQUENCE tag_track_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.tag_track_id_seq TO admin;


--
-- Name: TABLE tag_type; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.tag_type TO admin;


--
-- Name: TABLE track_org; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.track_org TO admin;

--
-- Name: TABLE upload_album; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.upload_album TO admin;

--
-- Name: TABLE upload_track; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.upload_track TO admin;


--
-- Name: TABLE upload_album_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.upload_album_search TO admin;


--
-- Name: TABLE upload_session; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.upload_session TO admin;


--
-- Name: SEQUENCE upload_session_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.upload_session_id_seq TO admin;


--
-- Name: TABLE upload_session_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.upload_session_search TO admin;


--
-- Name: TABLE upload_tracks_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.upload_tracks_search TO admin;


--
-- Name: TABLE workspace_org; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.workspace_org TO admin;


--
-- Name: TABLE workspace_pause; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.workspace_pause TO admin;


--
-- Name: TABLE workspace_search; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.workspace_search TO admin;


--
-- Name: TABLE ws_lib_tracks_to_be_synced; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON TABLE public.ws_lib_tracks_to_be_synced TO admin;


--
-- Name: SEQUENCE ws_lib_tracks_to_be_synced_id_seq; Type: ACL; Schema: public; Owner: admin
--

GRANT ALL ON SEQUENCE public.ws_lib_tracks_to_be_synced_id_seq TO admin;


--
-- Name: TABLE isrc_tunecode; Type: ACL; Schema: staging; Owner: admin
--

GRANT ALL ON TABLE staging.isrc_tunecode TO admin;


--
-- Name: TABLE staging_library; Type: ACL; Schema: staging; Owner: admin
--

GRANT ALL ON TABLE staging.staging_library TO admin;


--
-- Name: TABLE staging_tag_track; Type: ACL; Schema: staging; Owner: admin
--

GRANT ALL ON TABLE staging.staging_tag_track TO admin;


--
-- Name: SEQUENCE staging_tag_track_id_seq; Type: ACL; Schema: staging; Owner: admin
--

GRANT ALL ON SEQUENCE staging.staging_tag_track_id_seq TO admin;


--
-- Name: TABLE staging_workspace; Type: ACL; Schema: staging; Owner: admin
--

GRANT ALL ON TABLE staging.staging_workspace TO admin;


--
-- PostgreSQL database dump complete
--

