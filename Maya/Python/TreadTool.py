import maya.cmds as cmds
from contextlib import contextmanager
import logging
import re

# Setup logging
logging.basicConfig(level=logging.INFO, format='[%(levelname)s] %(message)s')
logger = logging.getLogger(__name__)

WINDOW_NAME = "treadMotionPathWin"
created_motionpaths = []

@contextmanager
def undo_chunk(name="tread_op"):
    try:
        cmds.undoInfo(openChunk=True, chunkName=name)
        yield
    finally:
        cmds.undoInfo(closeChunk=True)

def _ensure_exists(name):
    if not cmds.objExists(name):
        raise RuntimeError(f"Object does not exist: {name}")

def _unique_name(base):
    i = 1
    name = base
    while cmds.objExists(name):
        name = f"{base}_{i:03d}"
        i += 1
    return name

def _extract_joint_number(joint_name):
    """Extract number from joint name like 'l_tread05' -> 5"""
    match = re.search(r'tread(\d+)', joint_name)
    return int(match.group(1)) if match else None

def _list_motionpaths():
    return cmds.ls(type='motionPath') or []

def _find_new_motionpath(before_list, after_list):
    before_set = set(before_list)
    after_set = set(after_list)
    new = list(after_set - before_set)
    new.sort()
    return new

def _disable_parametric_length_on_node(mp_node):
    if not mp_node or not cmds.objExists(mp_node):
        return False
    attr = f"{mp_node}.fractionMode"
    if not cmds.objExists(attr):
        return False
    try:
        # Maya bug: 1 = OFF, 0 = ON
        cmds.setAttr(attr, 1)
        logger.info(f"Set {mp_node}.fractionMode = 1 (Parametric Length OFF)")
        return True
    except Exception as e:
        logger.warning(f"Could not set fractionMode on {mp_node}: {e}")
        return False

def _delete_addDoubleLinear_nodes(mp_node):
    """Delete addDoubleLinear nodes connected to motionPath"""
    hist = cmds.listHistory(mp_node, pruneDagObjects=True) or []
    add_nodes_from_hist = [n for n in hist if cmds.nodeType(n) == 'addDoubleLinear']

    add_nodes_from_conns = []
    connections = cmds.listConnections(mp_node, source=True, destination=True) or []
    for conn in connections:
        if cmds.objExists(conn) and cmds.nodeType(conn) == 'addDoubleLinear':
            if conn not in add_nodes_from_conns:
                add_nodes_from_conns.append(conn)

    all_add_nodes = list(set(add_nodes_from_hist + add_nodes_from_conns))

    if all_add_nodes:
        try:
            cmds.delete(all_add_nodes)
            logger.info(f"Deleted {len(all_add_nodes)} addDoubleLinear node(s)")
        except Exception as e:
            logger.warning(f"Could not delete addDoubleLinear nodes: {e}")

def _create_motion_path(joint, curve, world_up_object=None):
    """Create motion path for joint on curve"""
    kwargs = dict(follow=True, fa='z', ua='y', inverseUp=True, c=curve)
    if world_up_object:
        kwargs['wut'] = 'object'
        kwargs['wuo'] = world_up_object
    
    try:
        cmds.pathAnimation(joint, **kwargs)
    except RuntimeError as e:
        if "already has an input connection" in str(e):
            raise RuntimeError(f"Joint '{joint}' has existing connections")
        raise

def _find_created_motion_path(joint, before_list, after_list):
    """Find the motion path node that was just created"""
    new_nodes = _find_new_motionpath(before_list, after_list)
    
    if not new_nodes:
        conns = cmds.listConnections(f"{joint}.translate", source=True, plugs=True) or []
        new_nodes = [c.split('.')[0] for c in conns if cmds.nodeType(c.split('.')[0]) == 'motionPath']
    
    if not new_nodes:
        raise RuntimeError(f"Could not find motionPath for '{joint}'")
    
    return new_nodes

def _cleanup_motion_path(motion_path, joint):
    """Clean up motion path: disable parametric length, delete markers, connect outputs"""
    _disable_parametric_length_on_node(motion_path)
    
    # Delete positionMarker nodes
    position_markers = cmds.listConnections(motion_path, source=True, type='positionMarker') or []
    position_marker_shapes = cmds.listConnections(motion_path, source=True, shapes=True) or []
    
    all_markers = set(position_markers)
    for shape in position_marker_shapes:
        if cmds.nodeType(shape) == 'positionMarker':
            all_markers.add(shape)
            parents = cmds.listRelatives(shape, parent=True, fullPath=True) or []
            for parent in parents:
                all_markers.add(parent)
    
    if all_markers:
        try:
            cmds.delete(list(all_markers))
            logger.debug(f"Deleted {len(all_markers)} positionMarker node(s)")
        except Exception as e:
            logger.warning(f"Could not delete positionMarker nodes: {e}")
    
    # Connect allCoordinates to translate
    try:
        cmds.connectAttr(f"{motion_path}.allCoordinates", f"{joint}.translate", force=True)
        logger.debug(f"Connected {motion_path}.allCoordinates -> {joint}.translate")
    except Exception as e:
        logger.warning(f"Could not connect allCoordinates: {e}")
    
    _delete_addDoubleLinear_nodes(motion_path)

def _create_driven_key_curve(motion_path, cycle_plug, offset):
    """Create and connect driven key animation curve with offset"""
    uvalue_plug = f"{motion_path}.uValue"
    
    # Delete existing curves
    existing_curves = cmds.listConnections(uvalue_plug, source=True, type='animCurve') or []
    if existing_curves:
        cmds.delete(existing_curves)
    
    # Create driven key curve
    driven_curve = cmds.createNode('animCurveUL', name=_unique_name('tread_uValue_driven'))
    
    # Set keyframes with offset on INPUT
    input_at_0 = 0 + offset
    input_at_1 = 1 + offset
    output_at_0 = 0
    output_at_1 = 1
    
    cmds.setKeyframe(driven_curve, float=input_at_0, value=output_at_0)
    cmds.setKeyframe(driven_curve, float=input_at_1, value=output_at_1)
    
    # Set tangents and infinity
    cmds.keyTangent(driven_curve, edit=True, inTangentType='linear', outTangentType='linear')
    cmds.setAttr(f"{driven_curve}.preInfinity", 3)   # Cycle
    cmds.setAttr(f"{driven_curve}.postInfinity", 3)  # Cycle
    
    # Connect
    cmds.connectAttr(cycle_plug, f"{driven_curve}.input", force=True)
    cmds.connectAttr(f"{driven_curve}.output", uvalue_plug, force=True)
    
    logger.info(f"Created driven key: {cycle_plug} -> {motion_path}.uValue (offset {offset:.6f})")

def automate_tread_attach_single(j, curve, world_up_object=None,
                                 cycle_locator='treadCycle_loc',
                                 cycle_locator_plug='customOutput',
                                 total_joints=1):
    """Attach a single joint to the tread curve with motion path"""
    _ensure_exists(j)
    _ensure_exists(curve)
    
    if world_up_object and not cmds.objExists(world_up_object):
        logger.warning(f"World up object '{world_up_object}' not found, using scene up")
        world_up_object = None

    with undo_chunk(f"tread_attach_{j}"):
        # Extract joint number from name
        joint_number = _extract_joint_number(j)
        if joint_number is None:
            logger.warning(f"Could not extract number from '{j}', using 1")
            joint_number = 1
        
        logger.info(f"Processing: {j} (joint #{joint_number})")
        
        # Create motion path
        before = _list_motionpaths()
        _create_motion_path(j, curve, world_up_object)
        after = _list_motionpaths()
        new_nodes = _find_created_motion_path(j, before, after)

        # Clean up motion path nodes
        for mp in new_nodes:
            _cleanup_motion_path(mp, j)
            if mp not in created_motionpaths:
                created_motionpaths.append(mp)

        # Calculate offset
        offset = (joint_number - 1) / float(total_joints) if total_joints > 0 else 0
        logger.info(f"Calculated offset: {offset:.6f} (joint {joint_number}/{total_joints})")

        # Verify cycle locator
        if not cmds.objExists(cycle_locator):
            logger.error(f"Cycle locator '{cycle_locator}' not found")
            return
        
        if not cmds.attributeQuery(cycle_locator_plug, node=cycle_locator, exists=True):
            logger.error(f"Attribute '{cycle_locator_plug}' not found on {cycle_locator}")
            return
        
        cycle_plug = f"{cycle_locator}.{cycle_locator_plug}"

        # Setup driven keys for each motion path
        for mp in new_nodes:
            _create_driven_key_curve(mp, cycle_plug, offset)

        logger.info(f"Completed: {j}")

def automate_tread_attach(joint_list, curve, world_up_object=None,
                          cycle_locator='treadCycle_loc',
                          cycle_locator_plug='customOutput',
                          total_joints_override=None):
    global created_motionpaths
    created_motionpaths = []

    total_joints = total_joints_override if total_joints_override is not None else len(joint_list)
    logger.info(f"Starting tread automation for {len(joint_list)} joint(s), total joints: {total_joints}")

    for j in joint_list:
        try:
            automate_tread_attach_single(j, curve,
                                         world_up_object=world_up_object,
                                         cycle_locator=cycle_locator,
                                         cycle_locator_plug=cycle_locator_plug,
                                         total_joints=total_joints)
        except Exception as e:
            logger.error(f"Error processing {j}: {e}")

    # Final cleanup
    for mp in created_motionpaths:
        _disable_parametric_length_on_node(mp)
        _delete_addDoubleLinear_nodes(mp)
    
    logger.info(f"Automation complete: {len(created_motionpaths)} motionPath(s) created")

# ----------------------------
# UI Code
# ----------------------------

def _populate_curve_from_selection(field):
    sel = cmds.ls(selection=True) or []
    if sel:
        cmds.textField(field, edit=True, text=sel[-1])

def _populate_joints_from_selection(field):
    sel = cmds.ls(selection=True) or []
    if sel:
        cmds.textField(field, edit=True, text=",".join(sel))

def open_tread_motionpath_window():
    if cmds.window(WINDOW_NAME, exists=True):
        cmds.deleteUI(WINDOW_NAME)

    cmds.window(WINDOW_NAME, title="Tread MotionPath Automation", sizeable=False)
    main = cmds.columnLayout(adjustableColumn=True, rowSpacing=6, columnAlign="left", width=460)

    # Joints
    cmds.text(label="Joint(s) (comma-separated):", align='left')
    joint_field = cmds.textField(text="", width=380)
    cmds.rowLayout(numberOfColumns=2, columnWidth2=(220,220), parent=main)
    cmds.button(label="Use Selection as Joint(s)", 
                command=lambda *a: _populate_joints_from_selection(joint_field))
    cmds.button(label="Clear", 
                command=lambda *a: cmds.textField(joint_field, edit=True, text=""))
    cmds.setParent('..')

    # Total joints
    cmds.text(label="Total number of joints (for offset calculation):", align='left')
    total_joints_field = cmds.textField(text="", width=100)

    # Curve
    cmds.text(label="Tread Curve:", align='left')
    curve_field = cmds.textField(text="", width=380)
    cmds.rowLayout(numberOfColumns=2, columnWidth2=(220,220), parent=main)
    cmds.button(label="Use Selection as Curve", 
                command=lambda *a: _populate_curve_from_selection(curve_field))
    cmds.button(label="Clear", 
                command=lambda *a: cmds.textField(curve_field, edit=True, text=""))
    cmds.setParent('..')

    # World Up Object
    cmds.text(label="World Up Locator (optional):", align='left')
    wuo_field = cmds.textField(text="", width=380)
    cmds.rowLayout(numberOfColumns=2, columnWidth2=(220,220), parent=main)
    cmds.button(label="Use Selection as World Up", 
                command=lambda *a: cmds.textField(wuo_field, edit=True, 
                                                  text=cmds.ls(selection=True)[-1] if cmds.ls(selection=True) else ""))
    cmds.button(label="Clear", 
                command=lambda *a: cmds.textField(wuo_field, edit=True, text=""))
    cmds.setParent('..')

    # Cycle Locator
    cmds.text(label="Cycle Locator & Attribute:", align='left')
    cmds.rowLayout(numberOfColumns=3, columnWidth3=(160,160,140), parent=main)
    cycle_locator_field = cmds.textField(text="treadCycle_loc")
    cycle_attr_field = cmds.textField(text="treadCycle")
    cmds.button(label="Use Selection", 
                command=lambda *a: cmds.textField(cycle_locator_field, edit=True, 
                                                  text=cmds.ls(selection=True)[-1] if cmds.ls(selection=True) else ""))
    cmds.setParent('..')

    cmds.separator(height=8)
    cmds.rowLayout(numberOfColumns=3, columnWidth3=(150,150,150), parent=main)

    def on_run(*args):
        joints_text = cmds.textField(joint_field, query=True, text=True).strip()
        curve = cmds.textField(curve_field, query=True, text=True).strip()
        wuo = cmds.textField(wuo_field, query=True, text=True).strip() or None
        cycle_loc = cmds.textField(cycle_locator_field, query=True, text=True).strip()
        cycle_attr = cmds.textField(cycle_attr_field, query=True, text=True).strip()

        joints = [j.strip() for j in joints_text.split(',') if j.strip()] if joints_text else []
        if not joints:
            cmds.warning("No joints provided")
            return
        if not curve:
            cmds.warning("No curve provided")
            return

        total_joints_input = cmds.textField(total_joints_field, query=True, text=True).strip()
        total_joints = int(total_joints_input) if total_joints_input.isdigit() else len(joints)

        try:
            automate_tread_attach(joints, curve,
                                  world_up_object=wuo,
                                  cycle_locator=cycle_loc,
                                  cycle_locator_plug=cycle_attr,
                                  total_joints_override=total_joints)
        except Exception as e:
            cmds.confirmDialog(title='Error', 
                              message=f"Automation failed:\n{e}", 
                              button=['OK'],
                              icon='critical')

    def on_fill_from_selection(*args):
        sel = cmds.ls(selection=True) or []
        if not sel:
            cmds.warning("Select joint(s) then curve")
            return
        if len(sel) == 1:
            cmds.textField(curve_field, edit=True, text=sel[0])
        else:
            cmds.textField(curve_field, edit=True, text=sel[-1])
            cmds.textField(joint_field, edit=True, text=",".join(sel[:-1]))

    cmds.button(label="Fill from Selection", command=on_fill_from_selection)
    cmds.button(label="Run", command=on_run)
    cmds.button(label="Close", command=lambda *a: cmds.deleteUI(WINDOW_NAME, window=True))

    cmds.setParent('..')
    cmds.showWindow(WINDOW_NAME)
    try:
        cmds.window(WINDOW_NAME, edit=True, topLeftCorner=(50, 50))
    except:
        pass

if __name__ == "__main__":
    open_tread_motionpath_window()